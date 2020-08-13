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
using DigiBadges.Models;
using Microsoft.Extensions.Logging;

namespace DigiBadges.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly IEmailSender _emailSender;
        private IMongoCollection<Users> _usercollection;
        private readonly ILogger<ForgotPasswordModel> _logger;

        private MongoDbSetting _mongoDbOptions { get; set; }

        [TempData]
        public string EmailCode { get; set; }
        public Users user { get; set; }
        public ForgotPasswordModel(IEmailSender emailSender, IOptions<MongoDbSetting> mongoDbOptions, ILogger<ForgotPasswordModel> logger)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            _emailSender = emailSender;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this._usercollection = db.GetCollection<Users>(nameof(Users));
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Code { get; set; }

        }

        public IActionResult OnPostSubmitAsync()
        {
            if (ModelState.IsValid)
            {

                try
                {
                    user = _usercollection.Find(e => e.Email == Input.Email).FirstOrDefault();
                }
                catch (Exception e)
                {
                    ModelState.AddModelError(string.Empty, "Please try again later.");
                    _logger.LogError("ForgotPasswordError", e);
                    return Page();
                }
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Please try again with correct credentials.");
                    return Page();
                }
                if (EmailCode != null && EmailCode.Equals(Input.Code))
                {
                    TempData["Email"] = Input.Email;
                    return RedirectToPage("./ResetPassword");

                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Code did not match. Please try again.");
                    return Page();
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmCodeAsync(string vemail)
        {
            if (vemail == null)
            {
                ModelState["Code"].Errors.Clear();
                ModelState.AddModelError(string.Empty, "Please enter valid email.");
                return Page();
            }
            else
            {
                Input.Email = vemail;
            }
            try
            {
                user = _usercollection.Find(e => e.Email == vemail).FirstOrDefault();
                if (user != null)
                {
                    Random generator = new Random();
                    int code = generator.Next(100000, 1000000);
                    EmailCode = code.ToString();
                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your password change request.",
                               $"Your verification code is {code}. Please enter to change your password.");
                    ModelState.AddModelError(string.Empty, "Code sent on " + vemail + ".");
                }
            }

            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, "Please try again later.");
                _logger.LogError("ForgotPasswordError", e);
                return Page();
            }
            if (user == null)
            {
                ModelState["Code"].Errors.Clear();
                ModelState.AddModelError(string.Empty, "User does not exist in our directory!");
                return Page();
            }
            return Page();
        }
    }
}
