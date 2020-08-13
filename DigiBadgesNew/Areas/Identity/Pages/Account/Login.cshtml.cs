using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text;
using DigiBadges.Utility;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace DigiBadgesNew.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private IMongoCollection<RegisterModel.InputModel> collection;
        private MongoDbSetting _mongoDbOptions { get; set; }

        public LoginModel(SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<IdentityUser> userManager, IOptions<MongoDbSetting> mongoDbOptions)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.collection = db.GetCollection<RegisterModel.InputModel>("Register");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }
        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public IActionResult OnPost(InputModel input, string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                var email = input.Email;
                var password = encryptpass(input.Password);

                string encryptpass(string password)
                {
                    string msg = "";
                    byte[] encode = new byte[password.Length];
                    encode = Encoding.UTF8.GetBytes(password);
                    msg = Convert.ToBase64String(encode);
                    return msg;
                }
                RegisterModel.InputModel userEmail = collection.Find(e => e.Email == email).FirstOrDefault();
                if (userEmail != null)
                {
                    RegisterModel.InputModel userPassword = collection.Find(e => e.Password == password).FirstOrDefault();
                    if (userPassword != null)
                    {
                        TempData["Name"] = (userPassword.FirstName + userPassword.LastName);
                        return RedirectToAction("Index", "Home", new { area = "Employee" });
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid Credentials");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid Credentials");
                }

            }
            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
    }
