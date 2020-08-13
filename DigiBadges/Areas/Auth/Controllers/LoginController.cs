using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigiBadges.Utility;
using MongoDB.Driver;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DigiBadges.Areas.Identity.Pages.Account;
using DigiBadges.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Facebook;
using DigiBadges.Models;
using MongoDB.Bson;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

namespace DigiBadges.Areas.Auth.Controllers
{
    [Area("Auth")]
    public class LoginController : Controller
    {

        private readonly ILogger<LoginController> _logger;
        private readonly IEmailSender _emailSender;
        private IMongoCollection<Users> collection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        private IMongoCollection<UserRoles> _userRoleCollection;
        private IMongoCollection<LoginHistory> _LoginHistoryCol;
        public LoginHistory _loginHistory { get; set; }


        [BindProperty]
        public LoginVM Input { get; set; }

        public static string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }
        public Users _user { get; set; }
        IConfiguration configuration;
        public LoginController(ILogger<LoginController> logger, IConfiguration configuration, IOptions<MongoDbSetting> mongoDbOptions
                             , IEmailSender emailSender)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            _logger = logger;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.collection = db.GetCollection<Users>(nameof(Users));
            this._userRoleCollection = db.GetCollection<UserRoles>(nameof(UserRoles));
            _LoginHistoryCol = db.GetCollection<LoginHistory>(nameof(LoginHistory));
            _emailSender = emailSender;
            this.configuration = configuration;
        }
        // GET: LoginController
        public async Task<IActionResult> Index(string returnUrl = null)
        {
            Input = new LoginVM();
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect("/");
            }
            var rememberEmail = Request.Cookies["Email"];
            if (rememberEmail != null)
            {
                Input.Email = rememberEmail.Trim();
                Input.RememberMe = true;
            }
            else
            {
                Input.Email = string.Empty;
                Input.RememberMe = false;
            }

            // Clear the existing external cookie to ensure a clean login process            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ReturnUrl = returnUrl;
            return View(Input);
        }

        [Route("Login/{provider}")]
        public IActionResult SignIn(string provider, string returnUrl = null)
        {
            string[] providers = configuration.GetValue<string>("ExtAuthProviders").Split(',');
            if (!providers.Contains(provider))
            {
                return LocalRedirect("/Identity/Account/AccessDenied");
            }
            returnUrl = returnUrl ?? Url.Content("~/");
            ReturnUrl = returnUrl;
            returnUrl = "/Auth/Login/SetClaimsAsync";
            try
            {
                return Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" }, provider);
            }
            catch (Exception e)
            {
                _logger.LogError("LoginError", e);
                ErrorMessage = "Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<bool> SetCustomClaims()
        {

            if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email) == null)
            {
                return false;
            }
            string Email = User.FindFirst(ClaimTypes.Email).Value;
            _user = collection.Find(e => e.Email == Email).FirstOrDefault();
            if (_user == null)
            {
                return false;
            }
            //For Facebook          

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var role = _userRoleCollection.Find(e => e.RoleId == ObjectId.Parse(_user.RoleId)).FirstOrDefault().Role;

            await SetClaims(_user, role, false);

            return true;
        }
        /// <summary>
        /// Mehtod to set claims.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        private async Task SetClaims(Users user, string role, bool rememberMe)
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
                IsPersistent = rememberMe,
                ExpiresUtc = DateTime.UtcNow.AddDays(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            #endregion
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var email = Input.Email;
                    var password = AppUtility.Encrypt(Input.Password);

                    Users user = collection.Find(e => e.Email == email && e.Password == password).FirstOrDefault();

                    if (user != null && !string.IsNullOrEmpty(Input.Password))
                    {
                        if (user.IsUserVerified)
                        {
                            var role = _userRoleCollection.Find(e => e.RoleId == ObjectId.Parse(user.RoleId)).FirstOrDefault().Role;

                            #region claims
                            await SetClaims(user, role, Input.RememberMe);
                            #endregion

                            _logger.LogInformation("User {Email} logged in at {Time}.",
                                user.Email, DateTime.UtcNow);
                            if (Input.RememberMe)
                            {
                                var option = new CookieOptions();
                                option.Expires = DateTime.Now.AddDays(1);
                                Response.Cookies.Append("Email", Input.Email, option);
                            }
                            else
                            {
                                Response.Cookies.Delete("Email");
                            }
                            switch (role)
                            {
                                /*Admin*/
                                case AppUtility.AdminRole:
                                    return RedirectToAction("Index", "Users", new { area = "Admin" });

                                /*Earner*//*Issuer*/
                                default:
                                    return RedirectToAction("Index", "Home", new { area = "Employee" });
                            }
                        }
                        else
                        {
                            Random generator = new Random();
                            int code = generator.Next(100000, 1000000);
                            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                            $"Your verification code is {code}. Please enter to confrim your email");
                            TempData["email"] = user.Email;
                            TempData["verifycode"] = code;

                            return LocalRedirect("/Identity/Account/Verifycode");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid Credentials!");
                    }
                }
                // If we got this far, something failed, redisplay form                
                return View();
            }
            catch (Exception e)
            {
                _logger.LogError("LoginError", e);
                ModelState.AddModelError(string.Empty, "Please try again later.");
                return View();
            }
        }

        [Route("Auth/Login/SetClaimsAsync")]
        public async Task<IActionResult> SetClaimsAsync(string returnUrl = null)
        {
            try
            {
                returnUrl = ReturnUrl ?? Url.Content("~/");
                if (await SetCustomClaims())
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    ErrorMessage = "User not registered with our service! Please register first.";
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(nameof(Index));
                }

            }
            catch (Exception e)
            {
                ErrorMessage = "Please try again later.";
                _logger.LogError("LoginError", e);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
