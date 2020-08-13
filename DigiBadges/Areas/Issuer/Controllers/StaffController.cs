using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DigiBadges.DataAccess.Repository;
using DigiBadges.Models;
using DigiBadges.Models.ViewModels;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SolrNet;

namespace DigiBadges.Areas.Issuer.Controllers
{

    [Area(AppUtility.IssuerRole)]
    [Authorize]
    public class StaffController : Controller
    {

        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IEmailSender _emailSender;
        private IMongoCollection<Users> Users;
        private IMongoCollection<UserRoles> UserRoles;
        public Repository<DigiBadges.DataAccess.Users> _u;
        public Repository<DigiBadges.DataAccess.UserRoles> _ur;
        public Repository<DigiBadges.DataAccess.Issuers> _i;
        private IMongoCollection<Issuers> collection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        private readonly ISolrOperations<SolrUsersModel> _solr;
        private readonly ISolrOperations<SolrIssuersModel> _solrIssuer;

        public StaffController(IWebHostEnvironment hostEnvironment, IOptions<MongoDbSetting> mongoDbOptions,
            IEmailSender emailSender, Repository<DigiBadges.DataAccess.UserRoles> ur, Repository<DigiBadges.DataAccess.Users> u, Repository<DigiBadges.DataAccess.Issuers> i, ISolrOperations<SolrUsersModel> solr, ISolrOperations<SolrIssuersModel> solrIssuer)
        {
            _i = i;
            _u = u;
            _mongoDbOptions = mongoDbOptions.Value;
            _ur = ur;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            Users = db.GetCollection<Users>("Users");
            collection = db.GetCollection<Issuers>("Issuers");
            UserRoles = db.GetCollection<UserRoles>("UserRoles");
            _hostEnvironment = hostEnvironment;
            _emailSender = emailSender;
            _solr = solr;
            _solrIssuer = solrIssuer;
        }

        public async Task<IActionResult> Index(string id)
        {
            var userrolelist = UserRoles.Find(FilterDefinition<UserRoles>.Empty).ToList();
            StaffUsers staffUsers = new StaffUsers();
            TempData["isss"] = id;
            staffUsers.UserRoles = userrolelist;
            return View(staffUsers);
        }

        [HttpPost]
        public async Task<IActionResult> Index(StaffUsers staff)
        {
            try
            {
                //get the current issuer id
                var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;

                // Create object of staffUsers model
                StaffUsers staffUsers = new StaffUsers();
                staff.Users.IsUserVerified = true;
                staff.Users.Password = AppUtility.Encrypt("Welcome@123");
                staff.Users.CreatedDate = DateTime.Now;

                // find the issuer in the collection
                var issuerModel = collection.Find(e => e.UserId == new ObjectId(userid)).FirstOrDefault();
                if (issuerModel != null)
                {
                    staff.Users.CreatedBy = issuerModel.Name;

                }

                //check email of users already exists 
                var IsemailExists = Users.Find(e => e.Email == staff.Users.Email).ToList();
                if (IsemailExists.Count() > 0)
                {
                    ModelState.AddModelError(string.Empty, "User with this email already exist");
                    var userrolelist = UserRoles.Find(FilterDefinition<UserRoles>.Empty).ToList();
                    StaffUsers staffUsers1 = new StaffUsers();
                    staffUsers1.UserRoles = userrolelist;
                    return View(staffUsers1);
                }
                var useremail = Users.Find(e => e.Email == staff.Users.Email).FirstOrDefault();
                if (useremail == null)
                {
                    Users.InsertOne(staff.Users); // insert staff in user collection
                    SolrUsersModel su = new SolrUsersModel(staff.Users);
                    _solr.Add(su);
                    _solr.Commit();

                    var staffid = Users.Find(e => e.UserId == staff.Users.UserId).FirstOrDefault(); //get the staffid in user collections

                    string[] ids = new[] { staffid.UserId.ToString() };
                    Users[] staffobject = new[] { staff.Users };
                    //get the issuer in the issuer collection
                    var issuerModelnew = collection.Find(e => e.UserId == new ObjectId(userid)).FirstOrDefault();


                    if (issuerModelnew.StaffsIds != null && issuerModelnew.Staffsobject != null)
                    {
                        collection.UpdateOneAsync(x => x.UserId == new MongoDB.Bson.ObjectId(userid),
                         Builders<Issuers>.Update.PushEach(x => x.StaffsIds, ids)).ConfigureAwait(false); // push the staff id into the array of issuer staffids
                        collection.UpdateOneAsync(x => x.UserId == new MongoDB.Bson.ObjectId(userid),
                               Builders<Issuers>.Update.PushEach(x => x.Staffsobject, staffobject)).ConfigureAwait(false);
                    }
                    else
                    {
                        var filter = Builders<Issuers>.Filter.Eq("UserId", new ObjectId(userid));
                        var updateDef = Builders<Issuers>.Update.
                         Set("StaffsIds", ids);
                        updateDef = updateDef.
                        Set("Staffsobject", staffobject);
                        collection.UpdateOne(filter, updateDef); // update the staffids column 
                    }




                    if (issuerModel.StaffsIds != null && issuerModel.Staffsobject != null)
                    {
                        string[] staffArr = new string[issuerModel.StaffsIds.Length + 1];
                        Users[] staffObjArr = new Models.Users[issuerModel.Staffsobject.Length + 1];

                        for (int i = 0; i <= issuerModel.StaffsIds.Length - 1; i++)
                        {
                            staffArr[i] = issuerModel.StaffsIds[i];
                            staffObjArr[i] = issuerModel.Staffsobject[i];
                        }
                        staffArr[issuerModel.StaffsIds.Length] = ids[0];
                        staffObjArr[issuerModel.Staffsobject.Length] = staffobject[0];
                        issuerModel.StaffsIds = staffArr;
                        issuerModel.Staffsobject = staffObjArr;
                    }
                    else
                    {
                        issuerModel.StaffsIds = ids;
                        issuerModel.Staffsobject = staffobject;
                    }

                    issuerModelnew.UserId = new ObjectId(userid);
                    SolrIssuersModel sissuser = new SolrIssuersModel(issuerModelnew);
                    _solrIssuer.Add(sissuser);
                    _solrIssuer.Commit();

                    //send the email to the created staff 
                    await _emailSender.SendEmailAsync(staff.Users.Email,
                                   "Congratulation, you are invited as a staff",
                                   $"<h3 style = 'color:blueviolet' >Congratulation, you are invited.. for login</h3><div class='text-center'><a class='btn btn-secondary' href='http://13.90.135.29/Auth/Login'>Login your Account</a></div><br />" +
                                   $"" +
                                   $"<br/><h2>Your id - {staff.Users.Email}</h2><br/><h2>Your Password - {"Welcome@123"}</h2><br/></div><div class='col-3'></div></div>"
                                 );
                }

                // email exists then push the staff id into the array
                else
                {
                    var staffid = Users.Find(e => e.UserId == useremail.UserId).FirstOrDefault();
                    string[] ids = new[] { staffid.UserId.ToString() };
                    Users[] staffobject = new[] { staff.Users };
                    var issuerModel1 = collection.Find(e => e.UserId == new ObjectId(userid)).FirstOrDefault();
                    if (issuerModel1.StaffsIds != null)
                    {
                        collection.UpdateOneAsync(x => x.UserId == new MongoDB.Bson.ObjectId(userid),
                        Builders<Issuers>.Update.PushEach(x => x.StaffsIds, ids)).ConfigureAwait(false);
                        collection.UpdateOneAsync(x => x.UserId == new MongoDB.Bson.ObjectId(userid),
                                 Builders<Issuers>.Update.PushEach(x => x.Staffsobject, staffobject)).ConfigureAwait(false);
                    }
                    else
                    {
                        var filter = Builders<Issuers>.Filter.Eq("UserId", new ObjectId(userid));
                        var updateDef = Builders<Issuers>.Update.
                         Set("StaffsIds", ids);
                        updateDef = updateDef.
                        Set("Staffsobject", staffobject);
                        collection.UpdateOne(filter, updateDef);
                    }

                    /* SolrUsersModel su = new SolrUsersModel(staff.Users);
                     _solr.Add(su);
                     _solr.Commit();*/

                    if (issuerModel1.StaffsIds != null && issuerModel1.Staffsobject != null)
                    {
                        string[] staffArr = new string[issuerModel1.StaffsIds.Length + 1];
                        Users[] staffObjArr = new Models.Users[issuerModel1.Staffsobject.Length + 1];

                        for (int i = 0; i <= issuerModel1.StaffsIds.Length - 1; i++)
                        {
                            staffArr[i] = issuerModel1.StaffsIds[i];
                            staffObjArr[i] = issuerModel1.Staffsobject[i];
                        }
                        staffArr[issuerModel1.StaffsIds.Length] = ids[0];
                        staffObjArr[issuerModel1.Staffsobject.Length] = staffobject[0];
                        issuerModel1.StaffsIds = staffArr;
                        issuerModel1.Staffsobject = staffObjArr;
                    }
                    else
                    {
                        issuerModel1.StaffsIds = ids;
                        issuerModel1.Staffsobject = staffobject;
                    }

                    issuerModel1.UserId = new ObjectId(userid);
                    SolrIssuersModel sissuser = new SolrIssuersModel(issuerModel1);
                    _solrIssuer.Add(sissuser);
                    _solrIssuer.Commit();


                    //send email to the created staff
                    await _emailSender.SendEmailAsync(staff.Users.Email,
                                                "Congratulation, you are invited as a staff",

                                                 $"<h3 style = 'color:blueviolet' >Congratulation, you are invited.. for login</h3><div class='text-center'><a class='btn btn-secondary' href='http://13.90.135.29/Auth/Login'>Login your Account</a></div><br />" +
                                                 $"" +
                                                 $"<br/><h2>Your id - ${staff.Users.Email}</h2><br/><h2>Your Password - ${"Welcome@123"}</h2><br/></div><div class='col-3'></div></div>"
                                               );

                }
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Please try again later.");
                return View();
            }
            return RedirectToAction("ViewStaff");
        }


        public IActionResult ViewStaff()
        {
            try
            {
                //get current issuer id
                var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;
                ViewBag.issuerid = userid;

                // create a list
                List<Users> staffs = new List<Users>();
                var filter1 = Builders<Issuers>.Filter.Eq(s => s.UserId, new ObjectId(userid)); // filter the current issuer user id in issuer collection
                var issuerDocument = collection.Find(filter1).FirstOrDefault();

                if (issuerDocument.Staffsobject != null)
                {
                    for (var j = 0; j < issuerDocument.Staffsobject.Length; j++)
                    {

                        staffs.Add(issuerDocument.Staffsobject[j]);


                    }
                }

                IssuerBadge i = new IssuerBadge();
                i.users = staffs;
                return View(i);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Please try again later.");
                return View();
            }
        }


        public IActionResult EditStaff(string id)
        {
            var userToEdit = _u.FindById(id);
            return View(userToEdit);
        }


        [HttpPost]
        public IActionResult EditStaff(string id, DigiBadges.DataAccess.Users users)
        {
            var a = _u.FindById(id);
            a.FirstName = users.FirstName;
            a.LastName = users.LastName;
            a.Email = users.Email;


            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;

            DigiBadges.Models.Users usr = new DigiBadges.Models.Users()
            {
                //CreatedBy = usr.CreatedBy,                    

                FirstName = users.FirstName,
                LastName = users.LastName,
                Email = users.Email,
                UserId = new ObjectId(id),
                RoleId = a.RoleId,
                Password = a.Password,
                CreatedBy = a.CreatedBy,
                CreatedDate = a.CreatedDate,
                IsUserVerified = a.IsUserVerified
                //UserId = users.Id

            };

            SolrUsersModel su = new SolrUsersModel(usr);
            _solr.Add(su);
            _solr.Commit();


            List<DataAccess.Issuers> issuerlist1 = _i.FilterBy(e => e.UserId == new ObjectId(userid)).ToList();
            DataAccess.Issuers issuers = new DataAccess.Issuers();
            string issuerid = "";
            foreach (var item in issuerlist1)
            {
                issuerid = item.Id.ToString();
            }

            var issuerlist = _i.FindById(issuerid);
            DataAccess.Issuers i = new DataAccess.Issuers();
            i.Staffsobject = issuerlist.Staffsobject;
            i.Id = new ObjectId(issuerid);
            i.Image = issuerlist.Image;
            i.Name = issuerlist.Name;
            i.WebsiteUrl = issuerlist.WebsiteUrl;
            i.Description = issuerlist.Description;
            i.Email = issuerlist.Email;
            i.UserId = issuerlist.UserId;
            i.StaffsIds = issuerlist.StaffsIds;
            i.CreatedDate = issuerlist.CreatedDate;
            foreach (var j in i.Staffsobject)
            {
                if (j.Id == new ObjectId(id))
                {
                    j.FirstName = users.FirstName;
                    j.LastName = users.LastName;
                    j.Email = users.Email;
                    _i.ReplaceOne(i);
                }

            }

            _u.ReplaceOne(a);

            return RedirectToAction("ViewStaff");
        }
        public IActionResult DeleteStaff(string id)
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;  //claim[3].Value;
            var issuer = collection.Find(FilterDefinition<Issuers>.Empty).ToList();
            var userSpecificIssuer = issuer.Where(e => e.UserId == new ObjectId(userid)).FirstOrDefault();
            string[] allStaff = userSpecificIssuer.StaffsIds;

            var issuerStafftodelete = _i.FindById(userSpecificIssuer.IssuerId.ToString());

            string[] toDeleteStaff = new string[] { id };
            var mylist = new List<string>();
            var delsolrlist = new List<string>();
            List<string> staffidd = new List<string>();
            for (var j = 0; j < userSpecificIssuer.Staffsobject.Length; j++)
            {
                staffidd.Add(userSpecificIssuer.Staffsobject[j].UserId.ToString());
            }

            List<Users> us = new List<Users>();
            List<string> stafidobject = new List<string>();
            for (var i = 0; i < staffidd.Count(); i++)
            {
                if (staffidd.Count() == 1)
                {
                    string[] abc = staffidd.ToArray();
                    if (abc[i].ToString() == id.ToString())
                    {
                        string[] ids = null;

                        Users[] issu = new[] { userSpecificIssuer.Staffsobject[i] };
                        var stafffilter = Builders<Issuers>.Filter.Eq("UserId", new MongoDB.Bson.ObjectId(userid));
                        var staffupdateDef = Builders<Issuers>.Update.Set("Staffsobject", ids);
                        staffupdateDef = staffupdateDef.Set("StaffsIds", ids);
                        collection.UpdateOne(stafffilter, staffupdateDef);


                        // Users.DeleteOne<Users>(e => e.UserId == new ObjectId(id));
                    }
                }
                else
                {
                    Users[] issu = { userSpecificIssuer.Staffsobject[i] };
                    string[] stafidobj = { userSpecificIssuer.StaffsIds[i] };
                    string[] ids = null;
                    // Users.DeleteOne<Users>(e => e.UserId == new ObjectId(id));

                    if (issu.Length != null && stafidobj.Length != null)
                    {
                        if (issu[0].UserId != new ObjectId(id) && stafidobj[0] != id.ToString())
                        {
                            us.Add(userSpecificIssuer.Staffsobject[i]);
                            stafidobject.Add(userSpecificIssuer.StaffsIds[i]);
                        }
                    }
                }
            }

            var staffilter = Builders<Issuers>.Filter.Eq("UserId", new MongoDB.Bson.ObjectId(userid));
            var stafupdateDef = Builders<Issuers>.Update.Set("Staffsobject", us.ToArray());
            stafupdateDef = stafupdateDef.Set("StaffsIds", stafidobject.ToArray());
            collection.UpdateOne(staffilter, stafupdateDef);
            /*
                        if (id != null)
                        {
                            Users userMod = Users.Find(e => e.UserId == new ObjectId(id)).FirstOrDefault();
                            SolrUsersModel solUserMod = new SolrUsersModel(userMod);

                            _u.DeleteById(id);
                            _solr.Delete(solUserMod);
                            //Saving the changes 
                            _solr.Commit();

                            if (allStaff.Length == toDeleteStaff.Length)
                            {
                                userSpecificIssuer.StaffsIds = null;
                                SolrIssuersModel sissuser = new SolrIssuersModel(userSpecificIssuer);
                                //_solrIssuer.Delete(sissuser);
                                _solrIssuer.Add(sissuser);
                                //Saving the changes 
                                _solrIssuer.Commit();
                            }
                            else
                            {
                                for (int staffCount = 0; staffCount < allStaff.Length; staffCount++)
                                {
                                    if (allStaff[staffCount] != toDeleteStaff[0])
                                    {
                                        string[] s = new string[] { allStaff[staffCount] };
                                        delsolrlist.AddRange(s);
                                    }
                                }
                                string[] totalRemainingStaff = delsolrlist.ToArray();
                                userSpecificIssuer.StaffsIds = totalRemainingStaff;
                                SolrIssuersModel sissuser = new SolrIssuersModel(userSpecificIssuer);
                                //_solrIssuer.Delete(sissuser);
                                _solrIssuer.Add(sissuser);
                                //Saving the changes 
                                _solrIssuer.Commit();

                            }
                        }*/
            return RedirectToAction("ViewStaff");
        }

        public IActionResult SelfCreated()
        {
            var totalUser = _u.AsQueryable().ToList();
            var role = _ur.AsQueryable().ToList();
            var requiredRole = role.Where(e => e.Role == AppUtility.EarnerRole).FirstOrDefault();
            var earnerRoleUser = totalUser.Where(e => e.RoleId == requiredRole.Id.ToString()).ToList();
            //var userToDisplay = earnerRoleUser.Where(e => e.CreatedBy == AppUtility.DefaultCreatedBy).ToList();
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;  //claim[3].Value;
            var allIssuer = collection.Find(FilterDefinition<Issuers>.Empty).ToList();
            var thisIssuer = allIssuer.Where(e => e.UserId == new ObjectId(userid)).FirstOrDefault();

            var userToDisplay = earnerRoleUser.Where(e => (e.CreatedBy == AppUtility.DefaultCreatedBy) || (e.CreatedBy == AppUtility.AdminRole) || (e.CreatedBy != thisIssuer.Name)).ToList();

           

            string[] allStaff = thisIssuer.StaffsIds;
            if (allStaff != null)
            {
                var userAsSelfCreated = new List<DigiBadges.DataAccess.Users>();

                for (int self = 0; self < userToDisplay.Count; self++)
                {
                    bool b = false;

                    for (int staff = 0; staff < allStaff.Length; staff++)
                    {

                        if (allStaff[staff] == userToDisplay[self].Id.ToString())
                        {
                            b = true;
                            break;
                        }
                    }
                    if (b == false)
                    {
                        userAsSelfCreated.Add(userToDisplay[self]);
                    }
                }
                return View(userAsSelfCreated);
            }
            else
            {
                return View(userToDisplay);
            }

        }

        public IActionResult AddAsStaff(string id)
        {
            if (id != null)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
                var claim = claimsIdentity.Claims.ToArray();
                var email = claim[1].Value;
                var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;  //claim[3].Value;
                var thisUser = _u.FindById(userid.ToString());
                var issuer = collection.Find(FilterDefinition<Issuers>.Empty).ToList();
                var userSpecificIssuer = issuer.Where(e => e.UserId == new ObjectId(userid)).FirstOrDefault();
                var issuertoAddStaff = _i.FindById(userSpecificIssuer.IssuerId.ToString());
                var mylist = new List<string>();

                string[] availableStaffId = userSpecificIssuer.StaffsIds;
                string[] staffToAdd = new string[] { id };

                collection.UpdateOneAsync(x => x.UserId == new MongoDB.Bson.ObjectId(userid),
                        Builders<Issuers>.Update.PushEach(x => x.StaffsIds, staffToAdd)).ConfigureAwait(false);
                var staffToAdduserid = Users.Find(e => e.UserId == new ObjectId(id)).FirstOrDefault();

                Users[] newstaffobject = new Users[] { staffToAdduserid };
                if (availableStaffId == null)
                {
                    var staffilter = Builders<Issuers>.Filter.Eq("UserId", new MongoDB.Bson.ObjectId(userid));
                    var stafupdateDef = Builders<Issuers>.Update.Set("Staffsobject", newstaffobject);
                    stafupdateDef = stafupdateDef.Set("StaffsIds", staffToAdd.ToArray());
                    collection.UpdateOne(staffilter, stafupdateDef);
                }
                else
                {
                    Users[] staffobject = { staffToAdduserid };
                    collection.UpdateOneAsync(x => x.UserId == new MongoDB.Bson.ObjectId(userid),
                       Builders<Issuers>.Update.PushEach(x => x.Staffsobject, staffobject)).ConfigureAwait(false);
                }

                userSpecificIssuer.UserId = new ObjectId(id);
                userSpecificIssuer.StaffsIds = staffToAdd;
                /* SolrIssuersModel sissuser = new SolrIssuersModel(userSpecificIssuer);
                 _solrIssuer.Add(sissuser);
                 _solrIssuer.Commit();*/

                return RedirectToAction("SelfCreated");
            }
            return View();
        }

    }
}

