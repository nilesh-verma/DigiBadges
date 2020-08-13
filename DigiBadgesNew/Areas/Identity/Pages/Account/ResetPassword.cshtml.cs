using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DigiBadgesNew.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private IMongoCollection<RegisterModel.InputModel> collection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        public ResetPasswordModel(UserManager<IdentityUser> userManager, IOptions<MongoDbSetting> mongoDbOptions)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            _userManager = userManager;
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

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null)
        {
            
                return Page();
          
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var Email = Input.Email;
                var Password = encryptpass(Input.Password);

                string encryptpass(string password)
                {
                    string msg = "";
                    byte[] encode = new byte[password.Length];
                    encode = Encoding.UTF8.GetBytes(password);
                    msg = Convert.ToBase64String(encode);
                    return msg;
                }
                var ConfirmPassword = encryptpass(Input.ConfirmPassword);
                var filter = Builders<RegisterModel.InputModel>.Filter.Eq("Email", Email);
                var updateDef = Builders<RegisterModel.InputModel>.Update.Set("Password", Password);
                updateDef = updateDef.Set("ConfirmPassword", ConfirmPassword);
                var result = collection.UpdateOne(filter, updateDef);




                return RedirectToPage("./ResetPasswordConfirmation");



            }


            
            return Page();
        }
    }
}
