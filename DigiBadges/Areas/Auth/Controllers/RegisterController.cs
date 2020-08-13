using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DigiBadges.Models;
using DigiBadges.Models.ViewModels;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SolrNet;
using SolrNet.Utils;

namespace DigiBadges.Areas.Auth.Controllers
{
    [Area("Auth")]
    public class RegisterController : Controller
    {
        #region Properties
        private readonly ILogger<RegisterController> _logger;
        private IMongoCollection<Users> _userCollection;
        private IMongoCollection<UserRoles> _userRoleCollection;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IEmailSender _emailSender;
        private IMongoCollection<LoginHistory> _LoginHistoryCol;
        public LoginHistory _loginHistory { get; set; }
        private readonly ISolrOperations<SolrUsersModel> _solr;
        public static string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public string FirstName { get; set; }
        public string Email { get; set; }
        public string LastName { get; set; }

        [TempData]
        public string AuthProvider { get; set; }

        [BindProperty]
        public RegisterVM registerVM { get; set; }
        private MongoDbSetting _mongoDbOptions { get; set; }

        public Users _user { get; set; }
        #endregion

        #region  Constructor

        public RegisterController(IWebHostEnvironment hostEnvironment, IOptions<MongoDbSetting> mongoDbOptions,
            IEmailSender emailSender, ISolrOperations<SolrUsersModel> solr, ILogger<RegisterController> logger)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this._userCollection = db.GetCollection<Users>(nameof(Users));
            this._userRoleCollection = db.GetCollection<UserRoles>(nameof(UserRoles));
            _LoginHistoryCol = db.GetCollection<LoginHistory>(nameof(LoginHistory));
            _hostEnvironment = hostEnvironment;
            _emailSender = emailSender;
            _solr = solr;
            _logger = logger;
        }

        #endregion

        [HttpGet]
        public IActionResult Index(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect("/");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            Random generator = new Random();
            int code = generator.Next(100000, 1000000);
            TempData["verifycode"] = code;

            if (ModelState.IsValid)
            {
                var email = registerVM.Email;
                try
                {
                    #region register user

                    _user = _userCollection.Find(e => e.Email == email).FirstOrDefault();
                    if (_user != null)
                    {
                        ModelState.AddModelError(string.Empty, "Email already exists");
                        return View();
                    }
                    _user = new Users()
                    {
                        FirstName = registerVM.FirstName,
                        LastName = registerVM.LastName,
                        Email = registerVM.Email,
                        Password = AppUtility.Encrypt(registerVM.Password),
                        RoleId = _userRoleCollection.Find(e => e.Role == AppUtility.EarnerRole).FirstOrDefault().RoleId.ToString(),
                        CreatedDate = DateTime.Now,
                        CreatedBy = AppUtility.DefaultCreatedBy,
                        IsUserVerified = false
                    };

                    _userCollection.InsertOne(_user);
                    SolrUsersModel su = new SolrUsersModel(_user);
                    _solr.Add(su);
                    _solr.Commit();

                    #endregion
                }
                catch (Exception e)
                {
                    ErrorMessage = "Please try again later.";
                    _logger.LogError("RegisterError", e);
                    return RedirectToAction(nameof(Index));
                }

                await _emailSender.SendEmailAsync(_user.Email, "Confirm your email",
                    $"Your verification code is {code}. Please enter to confirm your email");
                TempData["email"] = _user.Email;
                return LocalRedirect("/Identity/Account/Verifycode");
            }
            // If we got this far, something failed, redisplay form
            return RedirectToAction(nameof(Index));
        }

        [Route("Register/{provider}")]
        public async Task<IActionResult> ExternalRegister(string provider, string returnUrl = null)
        {
            try
            {
                AuthProvider = provider;
                returnUrl = returnUrl ?? Url.Content("~/");
                ReturnUrl = returnUrl;
                returnUrl = "/Auth/Register/RegisterExternalUser";
                return Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" }, provider);
            }
            catch (Exception e)
            {
                ErrorMessage = "Please try again later.";
                _logger.LogError("RegisterError", e);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Index));
            }
        }

        [Route("Auth/Register/RegisterExternalUser")]
        public async Task<IActionResult> RegisterExternalUser(string returnUrl = null)
        {            
            returnUrl = ReturnUrl ?? Url.Content("~/");
            try
            {
                if (await RegisterUser())
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email) == null)
                    {
                        ErrorMessage = "Please use below form to register.";
                    }
                    else
                    {
                        ErrorMessage = "User already registered with our service! Please proceed to sign in.";
                    }

                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception e)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                ErrorMessage = "Please try again later.";
                _logger.LogError("RegisterError", e);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Social media register.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> RegisterUser()
        {
            var o = this.AuthProvider;
            if (User.Identity.IsAuthenticated)
            {
                if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email) != null)
                {
                    ExternalAuthMapper();
                    //Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;
                    //FirstName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value;
                    //LastName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname).Value;
                }
                else
                {
                    return false;
                }
            }
            _user = _userCollection.Find(e => e.Email == Email).FirstOrDefault();
            if (_user != null)
            {
                ModelState.AddModelError(string.Empty, "Email already exists");
                return false;
            }
            string userRoleId = _userRoleCollection.Find(e => e.Role == AppUtility.EarnerRole).FirstOrDefault().RoleId.ToString();
            _user = new Users()
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Password = string.Empty,
                RoleId = userRoleId,
                CreatedBy = AppUtility.DefaultCreatedBy,
                CreatedDate = DateTime.Now,
                IsUserVerified = true
            };

            _userCollection.InsertOne(_user);

            SolrUsersModel su = new SolrUsersModel(_user);
            _solr.Add(su);
            _solr.Commit();

            await SetClaims(_user, AppUtility.EarnerRole);
            return true;
        }

        /// <summary>
        /// Reading credentials from external providers.
        /// </summary>
        /// <returns></returns>
        private bool ExternalAuthMapper()
        {
            switch (AuthProvider)
            {
                case "Facebook":
                    return GetFacebookClaims();

                case "Twitter":
                    return GetTwitterClaims();
                default:
                    return GetFacebookClaims();
            }
        }

        private bool GetFacebookClaims()
        {
            Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;
            FirstName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName).Value;
            LastName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname).Value;
            return true;
        }

        private bool GetTwitterClaims()
        {
            Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;
            FirstName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;
            LastName = " ";
            return true;
        }

        /// <summary>
        /// Mehtod to set claims.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        private async Task SetClaims(Users user, string role)
        {
            _loginHistory = new LoginHistory
            {
                LoginTime = DateTime.Now,
                LogoutTime = DateTime.MinValue,
                UserId = user.UserId
            };
            _LoginHistoryCol.InsertOne(_loginHistory);

            #region claims
            var claims = new List<Claim>
                           {
                            new Claim(ClaimTypes.Name, string.Concat(user.FirstName," ", user.LastName)),
                            new Claim(ClaimTypes.Email ,user.Email ),
                            new Claim(ClaimTypes.Role, role),
                            new Claim(AppUtility.LoginHistoryId, _loginHistory.UserILoginID.ToString()),
                            new Claim(AppUtility.UserId, user.UserId.ToString())
                          };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            #endregion
        }
    }
}