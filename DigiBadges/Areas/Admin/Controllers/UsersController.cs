using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
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
using SolrNet;

namespace DigiBadgesNew.Areas.Admin.Controllers
{
    [Area(AppUtility.AdminRole)]
    [Authorize(Roles = AppUtility.AdminRole)]
    public class UsersController : Controller
    {
        private readonly IMongoCollection<Users> usersCollection;
        private readonly IMongoCollection<Issuers> issuersCollection;
        private readonly IMongoCollection<UserRoles> userRoleCollection;
        private readonly ISolrOperations<SolrUsersModel> _solr;
        private readonly IEmailSender _emailSender;

        private MongoDbSetting _mongoDbOptions { get; set; }
        public UsersController(IOptions<MongoDbSetting> mongoDbOptions, IEmailSender emailSender,ISolrOperations<SolrUsersModel> solr)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.usersCollection = db.GetCollection<Users>("Users");
            this.userRoleCollection = db.GetCollection<UserRoles>("UserRoles");
            this.issuersCollection = db.GetCollection<Issuers>("Issuers");
            _emailSender = emailSender;
             _solr = solr;
        }

        public IActionResult Index()
        {
            List<Users> getUsers = usersCollection.Find(users => true).ToList();
            List<UserRoles> userRole = userRoleCollection.Find(role => true).ToList();
            var userNo = 0;
            foreach(var users in getUsers)
            {
                var name = userRole.FirstOrDefault(e => e.RoleId == new ObjectId(users.RoleId));
                getUsers[userNo].RoleId =name.Role;
                userNo++;
            }
            UserVM userVM = new UserVM()
            {
                users = getUsers
            };
            
            return View(userVM);
        }

        public IActionResult Create()
        {
            CreateUser createUser = new CreateUser()
            {
                userRoles = userRoleCollection.Find(role=>role.Role!=AppUtility.IssuerRole).ToList()
            };
            return View(createUser);
        }
        //string encryptpass(string password)
        //{
        //    string msg = "";
        //    byte[] encode = new byte[password.Length];
        //    encode = Encoding.UTF8.GetBytes(password);
        //    msg = Convert.ToBase64String(encode);
        //    return msg;
        //}

        [HttpPost]
        public async Task<IActionResult> Create(Users users)
        {
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.Claims.ToArray();
                var loginUserEmail = claim[1].Value;
                var userName = claim[0].Value;

                var IsEmailExist = usersCollection.Find(e => e.Email == users.Email).ToList();
                if (IsEmailExist.Count() > 0)
                {
                    ModelState.AddModelError(string.Empty, "User with this email already exist");
                    CreateUser createUser = new CreateUser()
                    {
                        userRoles = userRoleCollection.Find(role => role.Role != AppUtility.IssuerRole).ToList()
                    };
                    return View(createUser);
                }

                DateTime today = DateTime.Now;
                var password = AppUtility.Encrypt(users.Password);
                users.CreatedDate = today;
                users.CreatedBy = userName;
                users.Password = password;
                users.IsUserVerified = true;
                usersCollection.InsertOne(users);

                SolrUsersModel su = new SolrUsersModel(users);
                _solr.Add(su);
                _solr.Commit();

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
                Users _user  = usersCollection.Find(e => e.UserId== userId).FirstOrDefault();
                var userRoleId = _user.RoleId;
                var userRole = userRoleCollection.Find(e => e.RoleId == new ObjectId(userRoleId)).FirstOrDefault().Role;

                var filterForUser = Builders<Users>.Filter.Eq("UserId", userId);
                
                var updateUsers = Builders<Users>.Update.Set("FirstName", user.users.FirstName);
                updateUsers = updateUsers.Set("LastName", user.users.LastName);
                updateUsers = updateUsers.Set("Email", user.users.Email);
                updateUsers = updateUsers.Set("RoleId", user.users.RoleId);
                updateUsers = updateUsers.Set("Password", AppUtility.Encrypt(user.users.Password));
                updateUsers = updateUsers.Set("CreatedBy", _user.CreatedBy);
                updateUsers = updateUsers.Set("CreatedDate", _user.CreatedDate);
                var result = usersCollection.UpdateOne(filterForUser, updateUsers);

                if(userRole == AppUtility.IssuerRole)
                {
                    var filterForIssuer = Builders<Issuers>.Filter.Eq("UserId", userId);
                    var updateIssuer = Builders<Issuers>.Update.Set("Name", user.users.FirstName + " " + user.users.LastName);
                    var resultForIssuers = issuersCollection.UpdateOne(filterForIssuer, updateIssuer);
                }

                  _user.UserId = userId;
                _user.FirstName =  user.users.FirstName;
                _user.LastName =  user.users.LastName;
                _user.Email =  user.users.Email;
                _user.RoleId =  user.users.RoleId;
                _user.Password =  user.users.Password;
                _user.CreatedBy =  user.users.CreatedBy;
                _user.CreatedDate =  user.users.CreatedDate;

                SolrUsersModel su = new SolrUsersModel(_user);
                _solr.Add(su);
                _solr.Commit(); 

                return RedirectToAction("Index");
            }

            return View();
        }

        public IActionResult Delete(string id)
        {
            ObjectId userId = new ObjectId(id);            
            var userRoleId = usersCollection.Find(e => e.UserId == userId).FirstOrDefault().RoleId;
            var userRole = userRoleCollection.Find(e => e.RoleId == new ObjectId(userRoleId)).FirstOrDefault().Role;

            if (userRole == AppUtility.IssuerRole)
            {
                issuersCollection.DeleteOne<Issuers>(item => item.UserId == userId);
            }
            Users user = usersCollection.Find(e => e.UserId == userId).FirstOrDefault();
            var delete = usersCollection.DeleteOne<Users>(e => e.UserId == userId);

            if (delete.DeletedCount > 0)
            {

                SolrUsersModel solUserMod = new SolrUsersModel(user);
                var results = _solr.Delete(solUserMod);
                //Saving the changes 
                _solr.Commit();

            }

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