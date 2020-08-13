using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DigiBadges.Areas.Identity.Pages.Account
{
    public class VerifycodeModel : PageModel
    {
        [BindProperty]
        public VerifyModel verify { get; set; }

        private IMongoCollection<Users> collection;
        private IMongoCollection<UserRoles> _userRoleCollection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        private readonly IEmailSender _emailSender;
        public Users AppUser { get; set; }
        private IMongoCollection<LoginHistory> _LoginHistoryCol;
        private readonly ILogger<VerifycodeModel> _logger;
        public LoginHistory _loginHistory { get; set; }

        public class VerifyModel
        {
            public string First { get; set; }
            public string Second { get; set; }
            public string Third { get; set; }
            public string Fourth { get; set; }
            public string Fifth { get; set; }
            public string Sixth { get; set; }
            public string Email { get; set; }

        }

        public VerifycodeModel(IOptions<MongoDbSetting> mongoDbOptions, IEmailSender emailSender, ILogger<VerifycodeModel> logger)
        {
            _emailSender = emailSender;
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.collection = db.GetCollection<Users>(nameof(Users));
            this._userRoleCollection = db.GetCollection<UserRoles>(nameof(UserRoles));
            _LoginHistoryCol = db.GetCollection<LoginHistory>(nameof(LoginHistory));
            _logger = logger;
        }
        public void OnGet()
        {
            if (verify != null)
            {
                verify.Email = TempData["email"].ToString();
            }
        }

        public async Task<IActionResult> OnPost(string id, string email)
        {

            var code = id;
            var userEmail = email;

            var number = verify.First + verify.Second + verify.Third + verify.Fourth + verify.Fifth + verify.Sixth;
            if (code == number)
            {
                try
                {
                    #region claims       
                    AppUser = collection.Find(e => e.Email == userEmail).FirstOrDefault();
                    var filter = Builders<Users>.Filter.Eq("Email", userEmail);
                    var updateDef = Builders<Users>.Update.Set("IsUserVerified", true);
                    var result = collection.UpdateOne(filter, updateDef);
                    var role = _userRoleCollection.Find(e => e.RoleId == ObjectId.Parse(AppUser.RoleId)).FirstOrDefault().Role;

                    _loginHistory = new LoginHistory
                    {
                        LoginTime = DateTime.Now,
                        LogoutTime = DateTime.MinValue,
                        UserId = AppUser.UserId
                    };
                    _LoginHistoryCol.InsertOne(_loginHistory);

                    var claims = new List<Claim>
                           {
                            new Claim(ClaimTypes.Name, string.Concat(AppUser.FirstName," ",AppUser.LastName)),
                            new Claim(ClaimTypes.Email,AppUser.Email ),
                            new Claim(ClaimTypes.Role, role),
                            new Claim(AppUtility.LoginHistoryId, _loginHistory.UserILoginID.ToString()),
                            new Claim(AppUtility.UserId,AppUser.UserId.ToString())
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
                catch (Exception e)
                {
                    ModelState.AddModelError(string.Empty, "Please try again later.");
                    TempData["email"] = email;
                    TempData["verifycode"] = code;
                    _logger.LogError("VerifyCodeError", e);
                    return Page();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Try again with correct credentials!");
                TempData["email"] = email;
                TempData["verifycode"] = code;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmCodeAsync(string email)
        {
            Random generator = new Random();

            int code = generator.Next(100000, 1000000);
            TempData["verifycode"] = code.ToString();
            TempData["email"] = email.ToString();
            await _emailSender.SendEmailAsync(email, "Please verify your account.",
                       $"Your verification code is {code}.");

            ModelState.AddModelError(string.Empty, "New code sent.");

            #region If required to send link in email.
            //var callbackUrl = Url.Page(
            //    "/Account/ConfirmEmail",
            //    pageHandler: null,
            //    values: new { userId = userId, code = code },
            //    protocol: Request.Scheme);
            //await _emailSender.SendEmailAsync(email, "Please verify your account.",
            //           $"Your verification code is {code}.<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            #endregion

            return Page();
        }
    }
}
