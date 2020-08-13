using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DigiBadges.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;
        private IMongoCollection<LoginHistory> _LoginHistoryCol;
        private MongoDbSetting _mongoDbOptions { get; set; }
        public LoginHistory _loginHistory { get; set; }

        public LogoutModel(ILogger<LogoutModel> logger, IOptions<MongoDbSetting> mongoDbOptions)
        {
            _logger = logger;
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);            
            _LoginHistoryCol = db.GetCollection<LoginHistory>(nameof(LoginHistory));
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var LoginHistoryId = User.Claims.FirstOrDefault(c => c.Type == AppUtility.LoginHistoryId).Value;
            var filter = Builders<LoginHistory>.Filter.Eq("_id", ObjectId.Parse(LoginHistoryId));
            var updateDef = Builders<LoginHistory>.Update.Set("LogoutTime", DateTime.Now);
            var result = _LoginHistoryCol.UpdateOne(filter, updateDef);
            _logger.LogInformation("User logged out.");
           
            return RedirectToAction("Index", "Login", new { area = "Auth" });
        }
    }
}
