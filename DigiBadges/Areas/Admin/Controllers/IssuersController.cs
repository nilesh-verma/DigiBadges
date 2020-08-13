using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DigiBadges.DataAccess.Repository;
using DigiBadges.Models;
using DigiBadges.Models.ViewModels;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SolrNet;

namespace DigiBadges.Areas.Admin.Controllers
{
    [Area(AppUtility.AdminRole)]
    [Authorize(Roles = AppUtility.AdminRole)]
    public class IssuersController : Controller
    {
        #region PROPERTIES

        private IMongoCollection<Issuers> collection;
        private readonly Repository<DataAccess.Users> _user;                  //User Collection
        private readonly Repository<DataAccess.UserRoles> _userRoles;         //UserRoles Collection
        private readonly Repository<DataAccess.Issuers> _i;                   //Issuer Collection
        private readonly Repository<DataAccess.Badge> _b;                   //Issuer Collection
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _hostEnvironment;                //For Image upload in project directory
        private readonly ISolrOperations<SolrUsersModel> _solr;               //Solr User Model
        private readonly ISolrOperations<SolrIssuersModel> _solrIssuer;       //Solr UserRole Model
        private MongoDbSetting _mongoDbOptions { get; set; }        //To get the connection string and database which we defined in AppSetting
        
        #endregion

        #region  CONSTRUCTOR
        public IssuersController(Repository<DataAccess.Badge> b, IEmailSender emailSender,Repository<DataAccess.UserRoles> userRoles, Repository<DataAccess.Issuers> i, IWebHostEnvironment hostEnvironment, Repository<DataAccess.Users> user, IOptions<MongoDbSetting> mongoDbOptions,ISolrOperations<SolrUsersModel> solr,ISolrOperations<SolrIssuersModel> solrIssuer)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            _b = b;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);   
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database); //To Establish connection with mongoDb
            this.collection = db.GetCollection<Issuers>("Issuers");           //To establish connection with Issuer Collection 
            _hostEnvironment = hostEnvironment;
            _user = user;
            _userRoles = userRoles;
            _i = i;
            _emailSender = emailSender;
             _solr = solr;
            _solrIssuer = solrIssuer;
        }

        #endregion

        #region CREATE AND GET ISSUERS
        public IActionResult Index(int productPage = 1)    
        {
            var useid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;   //To get the user id of LoggedIn user
            var issuer = collection.Find(FilterDefinition<Issuers>.Empty).ToList();           //Get Issuer Collection list
            var userSpecificIssuer = issuer.Where(e => e.UserId == new ObjectId(useid));      //To find issuer whose issuer UserID field will be equal to logedIn userid
            IssuerVM issuerVM = new IssuerVM()                                                //Creating object of ViewModel and passing issuer list   
            {
                issuers = issuer                                                              //we are using VM because we want to reflect two object in View.Cshtml
            };
            var count = issuerVM.issuers.Count();                                             //Counting total issuers in Issuer Collection
            issuerVM.issuers = issuerVM.issuers.OrderBy(p => p.Name)                         
                .Skip((productPage - 1) * 2).Take(2).ToList();                                //In order to take list of object according to productpage  

            issuerVM.PagingInfo = new PagingInfo                                              //For defining value of PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = 2,
                TotalItems = count,
                urlParam = "/Admin/Issuers/Index?productPage=:"
            };

            return View(issuerVM);
        }


        public IActionResult Create()
        {

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateAsync(Issuers issuers)
        {
            if (ModelState.IsValid)
            {


                try
                {
                    var claimsIdentity = (ClaimsIdentity)User.Identity;
                    var claim = claimsIdentity.Claims.ToArray();
                    var useid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;         //to get userId of loggedIn user
                    var userRole = _userRoles.AsQueryable().ToList();                                       //to get userRoleList
                    var issuerRoles = userRole.Where(e => e.Role == AppUtility.IssuerRole).FirstOrDefault();//find the object of issuer role

                    string webRootPath = _hostEnvironment.WebRootPath;
                    var files = HttpContext.Request.Form.Files;
                    if (files.Count > 0)
                    {
                        string fileName = Guid.NewGuid().ToString();
                        var uploads = Path.Combine(webRootPath, @"issuers");
                        var extenstion = Path.GetExtension(files[0].FileName);
                        using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                        {
                            files[0].CopyTo(filesStreams);
                        }
                        issuers.Image = @"\issuers\" + fileName + extenstion;

                    }
                    DateTime today = DateTime.Now;
                    var isEmailExistInUser = _user.FilterBy(e => e.Email == issuers.Email).ToList();
                    var isEmailExistInIssuer = collection.Find(e => e.Email == issuers.Email).ToList();
                    if (isEmailExistInIssuer.Count() > 0 || isEmailExistInUser.Count() > 0)
                    {
                        ModelState.AddModelError(string.Empty, "User with this email already exist");     //show popup if same email exists
                        return View();
                    }

                    DigiBadges.DataAccess.Users user = new DigiBadges.DataAccess.Users()
                    {
                        CreatedBy = claim[0].Value,
                        CreatedDate = today,
                        Email = issuers.Email,
                        FirstName = issuers.Name,
                        IsUserVerified = true,
                        Password = AppUtility.Encrypt(AppUtility.IssuerPassword),
                        RoleId = issuerRoles.Id.ToString()


                    };
                    _user.InsertOne(user);                                             //Inserting object in issuer table

                    DigiBadges.Models.Users users = new DigiBadges.Models.Users()
                    {
                        CreatedBy = claim[0].Value,
                        CreatedDate = today,
                        Email = issuers.Email,
                        FirstName = issuers.Name,
                        IsUserVerified = true,
                        Password = AppUtility.Encrypt(AppUtility.IssuerPassword),
                        RoleId = issuerRoles.Id.ToString(),
                        UserId = user.Id


                    };

                    SolrUsersModel su = new SolrUsersModel(users);
                     _solr.Add(su);                                    //Adding data in solr
                     _solr.Commit();

                    var userIdInUserTable = _user.AsQueryable().ToList();
                    var uid = userIdInUserTable.Where(e => e.Email == issuers.Email).FirstOrDefault();
                    if (user.Id != null)
                    {
                        issuers.UserId = user.Id;          //setting the userId which we got after inserting the above data in user collection
                        issuers.CreatedDate = today;
                    }

                    collection.InsertOne(issuers);         //To post the issuer object 

                    SolrIssuersModel sissuser = new SolrIssuersModel(issuers);
                    _solrIssuer.Add(sissuser);             //Adding data in solr     
                    _solrIssuer.Commit();

                    await _emailSender.SendEmailAsync(issuers.Email,                     //to send email to new issuer
                        "Congatulations you are invited as a issuer",
                        $"<h4 class='m-2'>Your Email id is -{HtmlEncoder.Default.Encode(issuers.Email)}</h4></div>" +
                        "Your password is - Welcome@123");
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "Please try again later.");
                    return View();
                }
                return RedirectToAction("Index");

            }

            return View();
        }

        #endregion

        #region Issuer Edit/Delete

        public IActionResult IssuersEdit(string id)
        {
            ObjectId oId = new ObjectId(id);

            Issuers issuers = collection.Find(e => e.IssuerId                   
        == oId).FirstOrDefault();
            return View(issuers);                                     //in order to render object which we wants to edit
        }

        [HttpPost]
        public IActionResult IssuersEdit(string id, Issuers issuer)
        {


            if (ModelState.IsValid)
            {
                
                    string webRootPath = _hostEnvironment.WebRootPath;
                    var files = HttpContext.Request.Form.Files;

                    ObjectId oId = new ObjectId(id);

                    Issuers issuers = collection.Find(e => e.IssuerId
                == oId).FirstOrDefault();



                    if (files.Count > 0)
                    {
                        string fileName = Guid.NewGuid().ToString();
                        var uploads = Path.Combine(webRootPath, @"issuers");
                        var extenstion = Path.GetExtension(files[0].FileName);

                        if (issuers.Image != null)
                        {
                            //this is an edit and we need to remove old image
                            var imagePath = Path.Combine(webRootPath, issuers.Image.TrimStart('\\'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }
                        using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                        {
                            files[0].CopyTo(filesStreams);
                        }
                        issuer.Image = @"\issuers\" + fileName + extenstion;
                    }
                    else
                    {
                        //update when they do not change the image
                        if (issuer.IssuerId != null)
                        {

                            issuer.Image = issuers.Image;
                        }
                    }

                    var usr = _user.FindById(issuers.UserId.ToString());
                    usr.Email = issuer.Email;
                    usr.FirstName = issuer.Name;

                    _user.ReplaceOne(usr);                                 //updating data in user collection              




                    var filter = Builders<Issuers>.Filter.Eq("IssuerId", oId);
                    var updateDef = Builders<Issuers>.Update.
                Set("Name", issuer.Name);
                    updateDef = updateDef.Set("Email", issuer.Email);
                    updateDef = updateDef.Set("WebsiteUrl", issuer.WebsiteUrl);                //updating data in Issuer collection
                    updateDef = updateDef.Set("Image", issuer.Image);
                    updateDef = updateDef.Set("Description", issuer.Description);
                    var result = collection.UpdateOne(filter, updateDef);

                DigiBadges.Models.Users users = new DigiBadges.Models.Users()
                {
                    CreatedBy = usr.CreatedBy,                    
                    Email = issuer.Email,
                    FirstName = issuer.Name,
                    IsUserVerified = usr.IsUserVerified,
                    Password =usr.Password,
                    RoleId = usr.RoleId,
                    UserId = usr.Id

                };

                SolrUsersModel su = new SolrUsersModel(users);
                _solr.Add(su);
                _solr.Commit();
               
                issuer.IssuerId = oId;
                issuer.UserId = usr.Id;
                SolrIssuersModel sissuser = new SolrIssuersModel(issuer);
                _solrIssuer.Add(sissuser);
                _solrIssuer.Commit();

                return RedirectToAction("Index");






            }

            return View();

        }


        public IActionResult IssuersDelete(string id)
        {
            ObjectId oId = new ObjectId(id);
            var issuer = _i.FindById(id);

            var users = _user.AsQueryable().ToList();
            var userToDelete = users.Where(e => e.Id == issuer.UserId).FirstOrDefault();   

            DigiBadges.Models.Users usrMod = new DigiBadges.Models.Users()
                {
                    CreatedBy = userToDelete.CreatedBy,
                    CreatedDate = userToDelete.CreatedDate,
                    Email = userToDelete.Email,
                    FirstName = userToDelete.FirstName,
                    IsUserVerified = userToDelete.IsUserVerified,
                    Password = userToDelete.Password,
                    RoleId = userToDelete.RoleId,
                    UserId = userToDelete.Id
                };

            DigiBadges.Models.Issuers issMod = new DigiBadges.Models.Issuers()
                {
                    IssuerId = issuer.Id,
                    Image = issuer.Image,
                    Name = issuer.Name,
                    WebsiteUrl = issuer.WebsiteUrl,
                    Email = issuer.Email,
                    Description = issuer.Description,
                    UserId = issuer.UserId,
                    StaffsIds = issuer.StaffsIds,  
                    CreatedDate = issuer.CreatedDate
                };

            SolrUsersModel solUserMod = new SolrUsersModel(usrMod);
            SolrIssuersModel sissuser = new SolrIssuersModel(issMod);



            _user.DeleteById(userToDelete.Id.ToString());                        //Deleting user from user collection
            var result = collection.DeleteOne<Issuers>(e => e.IssuerId == oId);  //Deleting user from issuer collection

              if (result.DeletedCount > 0)
            {

              
                var results = _solr.Delete(solUserMod);
                //Saving the changes 
                _solr.Commit();

             

                _solrIssuer.Delete(sissuser);                
                _solrIssuer.Commit();

            }
            
            var badges = _b.FilterBy(e => e.IssuerId == issuer.Id).ToList();
            if(badges != null)
            {
                _b.DeleteMany(e => e.IssuerId == issuer.Id);
            }
            
            return RedirectToAction("Index");
        }

        #endregion
    }

}