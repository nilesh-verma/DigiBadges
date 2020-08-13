using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigiBadges.Models;
using DigiBadges.Models.ViewModels;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DigiBadgesNew.Areas.Admin.Controllers
{
    [Area(AppUtility.AdminRole)]
    [Authorize(Roles = AppUtility.AdminRole)]
    public class HomeController : Controller
    {
        private IMongoCollection<Users> usersCollection;
        private IMongoCollection<UserRoles> userRoleCollection;
        private readonly IEmailSender _emailSender;

        private MongoDbSetting _mongoDbOptions { get; set; }
        public HomeController(IOptions<MongoDbSetting> mongoDbOptions, IEmailSender emailSender)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.usersCollection = db.GetCollection<Users>("Users");
            this.userRoleCollection = db.GetCollection<UserRoles>("UserRoles");
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult create()
        {
            CreateUser createuser = new CreateUser()
            {
                userRoles = userRoleCollection.Find(role => true).ToList()
            };
            return View(createuser);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Users users)
        {
            if (ModelState.IsValid)
            {
                DateTime today = DateTime.Now;
                users.CreatedDate = today;
                usersCollection.InsertOne(users);
                await _emailSender.SendEmailAsync(users.Email, "Congratulation, Now you can use DigiBadges",

                          $"LoginId: {users.Email}<br/>Password: {users.Password}"
                    );
                return RedirectToAction("Index");
            }
            return View();
        }

            public IActionResult Edit(string id)
            {
                ObjectId userId = new ObjectId(id);
                Users user = usersCollection.Find(e => e.UserId == userId).FirstOrDefault();
                CreateUser createUser = new CreateUser()
                {
                    userRoles = userRoleCollection.Find(role => true).ToList(),
                    users =user
                };
                return View(createUser);
            }

        [HttpPost]
        public IActionResult Edit(string id, CreateUser user)
        {
            if (ModelState.IsValid)
            {
                ObjectId userId = new ObjectId(id);
                Users _user = usersCollection.Find(e => e.UserId == userId).FirstOrDefault();

                var filter = Builders<Users>.Filter.Eq("UserId", userId);
                var updateUsers = Builders<Users>.Update.Set("FirstName", user.users.FirstName);
                updateUsers = updateUsers.Set("LastName", user.users.LastName);
                updateUsers = updateUsers.Set("Email", user.users.Email);
                updateUsers = updateUsers.Set("RoleId", user.users.RoleId);
                updateUsers = updateUsers.Set("Password", user.users.Password);
                updateUsers = updateUsers.Set("CreatedBy", _user.CreatedBy);
                updateUsers = updateUsers.Set("CreatedDate", _user.CreatedDate);
                var result = usersCollection.UpdateOne(filter, updateUsers);

                return RedirectToAction("Index");
            }

            return View();
        }

        public IActionResult Delete(string id)
        {
            ObjectId userId = new ObjectId(id);
            var delete = usersCollection.DeleteOne<Users>(e => e.UserId == userId);

            if (delete.IsAcknowledged)
            {
                TempData["Message"] = "Role deleted successfully!";
            }
            else
            {
                TempData["Message"] = "Error while deleting Rolee!";
            }
            return RedirectToAction("Index");
        }

    }
}