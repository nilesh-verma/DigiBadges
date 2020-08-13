using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace DigiBadgesNew.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private IMongoCollection<InputModel> collection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender, IOptions<MongoDbSetting> mongoDbOptions)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.collection = db.GetCollection<InputModel>("Register");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement]
            [Required]
            public string FirstName { get; set; }

            [BsonElement]
            [Required]
            public string LastName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        }

        public async Task<IActionResult> OnPostAsync(InputModel input, string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            Random generator = new Random();
            int code = generator.Next(100000, 1000000);
            TempData["verifycode"] = code;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var email = input.Email;
                TempData["email"] = email;
                var password = encryptpass(input.Password);
                InputModel Input = collection.Find(e => e.Email == email).FirstOrDefault();
                string encryptpass(string password)
                {
                    string msg = "";
                    byte[] encode = new byte[password.Length];
                    encode = Encoding.UTF8.GetBytes(password);
                    msg = Convert.ToBase64String(encode);
                    return msg;
                }
                InputModel newinput = new InputModel()
                {
                    Id = input.Id,
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Email = input.Email,
                    Password = password,
                    ConfirmPassword = password
                };

                if (Input == null)
                {
                    collection.InsertOne(newinput);
                    await _emailSender.SendEmailAsync(input.Email, "Confirm your email",
                        $"Your verification code is {code}. Please enter to confrim your email");
                    return RedirectToPage("verifycode");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email already exists");
                }
            }
            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
