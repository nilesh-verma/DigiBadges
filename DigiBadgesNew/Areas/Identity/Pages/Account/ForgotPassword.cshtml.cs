using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using DigiBadges.Utility;

namespace DigiBadgesNew.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private IMongoCollection<RegisterModel.InputModel> collection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender, IOptions<MongoDbSetting> mongoDbOptions)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            _userManager = userManager;
            _emailSender = emailSender;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.collection = db.GetCollection<RegisterModel.InputModel>("Register");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {

                RegisterModel.InputModel userEmail = collection.Find(e => e.Email == Input.Email).FirstOrDefault();

                if (userEmail == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                return RedirectToPage("./ResetPassword");
            }

               
            return Page();
        }
    }
}
