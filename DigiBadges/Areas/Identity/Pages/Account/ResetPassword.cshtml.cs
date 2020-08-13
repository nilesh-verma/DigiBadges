using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DigiBadges.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private IMongoCollection<Users> collection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ResetPasswordModel> _logger;


        [TempData]
        public string Passcode { get; set; }

        public ResetPasswordModel(IOptions<MongoDbSetting> mongoDbOptions, IEmailSender emailSender, ILogger<ResetPasswordModel> logger)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.collection = db.GetCollection<Users>("Users");
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

        }
        public string code { get; set; }
        public IActionResult OnGet(string code = null)
        {
            if (string.IsNullOrEmpty(Email) && TempData["Email"] != null)
            {
                Email = TempData["Email"].ToString();
            }

            return Page();
        }

        public IActionResult OnPostSuccessAsync()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var filter = Builders<Users>.Filter.Eq("Email", Email);
                    var updateDef = Builders<Users>.Update.Set("Password", AppUtility.Encrypt(Input.Password));
                    var result = collection.UpdateOne(filter, updateDef);
                    return RedirectToPage("./ResetPasswordConfirmation");
                }
                catch (Exception e)
                {
                    TempData["Email"] = Email;
                    ModelState.AddModelError(string.Empty, "Please try again later.");
                    _logger.LogError("ResetPasswordError", e);
                    return Page();
                }
            }
            return Page();
        }
    }
}
