using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DigiBadgesNew.Areas.Admin.Controllers
{
    [Area(AppUtility.AdminRole)]
    [Authorize(Roles = AppUtility.AdminRole)]
    public class UserRoleController : Controller
    {
        private readonly IMongoCollection<UserRoles> userRoleCollection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        public UserRoleController(IOptions<MongoDbSetting> mongoDbOptions)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.userRoleCollection = db.GetCollection<UserRoles>("UserRoles");
        }

        public IActionResult Index()
        {
            List<UserRoles> getRoles = userRoleCollection.Find(role => true).ToList();

            return View(getRoles);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(UserRoles userRoles)
        {
            if (ModelState.IsValid)
            {
                userRoleCollection.InsertOne(userRoles);
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Edit(string id)
        {
            ObjectId roleId = new ObjectId(id);
            UserRoles userRoles = userRoleCollection.Find(e => e.RoleId == roleId).FirstOrDefault();
            return View(userRoles);
        }

        [HttpPost]
        public IActionResult Edit(string id, UserRoles userRoles)
        {
            if (ModelState.IsValid)
            {
                ObjectId roleId = new ObjectId(id);

                var filter = Builders<UserRoles>.Filter.Eq("RoleId", roleId);
                var updateUserRole = Builders<UserRoles>.Update.Set("Role", userRoles.Role);
                var result = userRoleCollection.UpdateOne(filter, updateUserRole);

                return RedirectToAction("Index");
            }

            return View();
        }

        public IActionResult Delete(string id)
        {
            ObjectId roleId = new ObjectId(id);
            var delete = userRoleCollection.DeleteOne<UserRoles>(e => e.RoleId == roleId);

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
