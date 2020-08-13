/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DigiBadges.Models;
using DigiBadges.Models.ViewModels;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DigiBadgesNew.Areas.Issuer.Controllers
{
    [Area("Issuer")]

    public class IssuersController : Controller
    {
        private IMongoCollection<Issuers> collection;
        private IMongoCollection<Badge> badgeCollection;
        private IMongoCollection<Badge_User_Collection> badgeUserCollection;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IEmailSender _emailSender;
        private  MongoDbSetting _mongoDbOptions { get; set; }
        public IssuersController(IWebHostEnvironment hostEnvironment,IOptions<MongoDbSetting> mongoDbOptions,
            IEmailSender emailSender)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.collection = db.GetCollection<Issuers>("Issuers");
            this.badgeCollection = db.GetCollection<Badge>("Badges");
            this.badgeUserCollection = db.GetCollection<Badge_User_Collection>("Badge_User_Collection");
            _hostEnvironment = hostEnvironment;
            _emailSender = emailSender;
        }
        public IActionResult Index(int productPage=1)
        {
            IssuerVM issuerVM = new IssuerVM()
            {
                issuers = collection.Find(FilterDefinition<Issuers>.Empty).ToList()
            };
            var count = issuerVM.issuers.Count();
            issuerVM.issuers = issuerVM.issuers.OrderBy(p => p.Name)
                .Skip((productPage - 1) * 2).Take(2).ToList();

            issuerVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = 2,
                TotalItem = count,
                urlParam = "/Issuer/Issuer/Index?productPage=:"
            };

            return View(issuerVM);
        }

        #region Creating Issuers 
        public IActionResult Create()
        {

            return View();
        }
        [HttpPost]
        public IActionResult Create(Issuers issuers)
        {
            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"Images\issuers");
                    var extenstion = Path.GetExtension(files[0].FileName);

                    //if (issuers.ImagePath != null)
                    //{
                    //    //this is an edit and we need to remove old image
                    //    var imagePath = Path.Combine(webRootPath,issuers.ImagePath.TrimStart('\\'));
                    //    if (System.IO.File.Exists(imagePath))
                    //    {
                    //        System.IO.File.Delete(imagePath);
                    //    }
                    //}
                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    issuers.ImagePath = @"\images\issuers\" + fileName + extenstion;
                }
                collection.InsertOne(issuers);
                return RedirectToAction("Index");

            }

            return View();
        }

        #endregion

        public IActionResult Issuers(string id)
        {
           
            ObjectId oId = new ObjectId(id);
            Badge b = badgeCollection.Find(e => e.IssuerId
        == oId).FirstOrDefault();
            int count=0;
            Issuers issuers = collection.Find(e => e.Id
        == oId).FirstOrDefault();
            IEnumerable<Badge> badge = badgeCollection.Find(e => e.IssuerId == oId).ToList();
            

            if (b == null)
            {
                count = 0;
            }
            else
            {
                IEnumerable<Badge_User_Collection> badge_User_Collections = badgeUserCollection.Find(e => e.BadgeId == b.BadgeId).ToList();
                count = badge_User_Collections.Count();
            }
                IssuerBadge issuerBadge = new IssuerBadge()
            {
                Id = id,
                issuer = issuers,
                badge = badge,
                Badge_Count= count


            };
            return View(issuerBadge);
        }

        public IActionResult Badge(string id)
        {
          // Id = MongoDB.Bson.ObjectId.Parse(id)
            IssuerBadge issuerBadge = new IssuerBadge()
            {
                Id = id
            };
            List<string> Num = new List<string>() { "Days","Week","Month","Years"};
            ViewBag.Number = new SelectList(Num);
            return View(issuerBadge);
        }

        [HttpPost]
        public IActionResult Badge(string Select,IssuerBadge issuerBadge)
        {
            
            if (ModelState.IsValid)
            {
                double d = Math.Ceiling(issuerBadge.badge1.ExpiryDate);
                switch (Select)
                {
                    case "Days":
                        {
                            issuerBadge.badge1.ExpiryDate = d;


                        }
                        break;
                    case "Week":
                        {
                            issuerBadge.badge1.ExpiryDate = d*7;


                        }
                        break;
                    case "Months":
                        {
                            issuerBadge.badge1.ExpiryDate = d*30;
                        }
                        break;

                    case "Years":
                        {
                            issuerBadge.badge1.ExpiryDate = d*365;
                        }
                        break;

                }

                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"Images\badges");
                    var extenstion = Path.GetExtension(files[0].FileName);


                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }



                    issuerBadge.badge1.ImageUrl = @"\images\badges\" + fileName + extenstion;
                    
                }

                Badge b = new Badge()
                {
                    IssuerId = ObjectId.Parse(issuerBadge.Id),
                    BadgeName = issuerBadge.badge1.BadgeName,
                    ImageUrl = issuerBadge.badge1.ImageUrl,
                    ExpiryDate = issuerBadge.badge1.ExpiryDate,
                    Description = issuerBadge.badge1.Description,
                    CreatedOn=DateTime.Now,
                    facebookId = issuerBadge.badge1.facebookId
                };
                badgeCollection.InsertOne(b);
                return RedirectToAction("Index");

            }

            return View();
        }

        public IActionResult ViewBadge(string id)
        { 

            ObjectId oId = new ObjectId(id);

            Badge badge = badgeCollection.Find(e => e.BadgeId
        == oId).FirstOrDefault();
            ObjectId iid = badge.IssuerId;
            Issuers issuers = collection.Find(e => e.Id
        == iid).FirstOrDefault();

            if (badge != null)
            {
                TempData["badgetest"] = badge.facebookId;
            }

            IEnumerable<Badge_User_Collection> badge_User = badgeUserCollection.Find(e => e.BadgeId
       == oId).ToList();

            IssuerBadge i = new IssuerBadge()
            {
                badge1 = badge,
                issuer = issuers,
                Badge_User_Collections=badge_User
            };
            return View(i);
        }

        #region Issuer Edit/Delete

        public IActionResult IssuersEdit(string id)
        {
            ObjectId oId = new ObjectId(id);

            Issuers issuers = collection.Find(e => e.Id
        == oId).FirstOrDefault();
            return View(issuers);
        }

        [HttpPost]
        public IActionResult IssuersEdit(string id,Issuers issuer)
        {


            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;

                ObjectId oId = new ObjectId(id);

                Issuers issuers = collection.Find(e => e.Id
            == oId).FirstOrDefault();


                
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"images/issuers");
                    var extenstion = Path.GetExtension(files[0].FileName);

                    if (issuer.ImagePath != null)
                    {
                        //this is an edit and we need to remove old image
                        var imagePath = Path.Combine(webRootPath, issuer.ImagePath.TrimStart('\\'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    issuer.ImagePath = @"\images/issuers\" + fileName + extenstion;
                }
                else
                {
                    //update when they do not change the image
                    if (issuer.Id != null)
                    {
                       
                        issuer.ImagePath = issuers.ImagePath;
                    }
                }



                var filter = Builders<Issuers>.Filter.Eq("Id", oId);
                var updateDef = Builders<Issuers>.Update.
            Set("Name", issuer.Name);
                updateDef = updateDef.Set("Email", issuer.Email);
                updateDef = updateDef.Set("WebUrl", issuer.WebUrl);
                updateDef = updateDef.Set("ImagePath", issuer.ImagePath);
                updateDef = updateDef.Set("Description", issuer.Description);
                var result = collection.UpdateOne(filter, updateDef);


                return RedirectToAction("Index");



                //if (files.Count > 0 && files[0] != null)
                //{
                //    //if user uploads a new image
                //    var uploads = Path.Combine(webRootPath, "images/issuers");
                //    var extension_new = Path.GetExtension(files[0].FileName);
                //    var extension_old = Path.GetExtension(issuers.ImagePath);

                //    if (System.IO.File.Exists(Path.Combine(uploads, issuers.Id + extension_old)))
                //    {
                //        System.IO.File.Delete(Path.Combine(uploads, issuers.Id + extension_old));
                //    }
                //    using (var filestream = new FileStream(Path.Combine(uploads, issuers.Id + extension_new), FileMode.Create))
                //    {
                //        files[0].CopyTo(filestream);
                //    }
                //    issuer.ImagePath = @"\" + "images/issuers" + @"\" + issuers.Id + extension_new;
                //}

                //if (issuer.ImagePath != null)
                //{
                //    issuer.ImagePath = issuers.ImagePath;
                //}


            }

            return View();
            
        }


        public IActionResult IssuersDelete(string id)
        {
            ObjectId oId = new ObjectId(id);
            var badge = badgeCollection.DeleteMany<Badge>(e => e.IssuerId == oId);
            var result = collection.DeleteOne<Issuers>
        (e => e.Id == oId);

            if (result.IsAcknowledged)
            {
                TempData["Message"] = "Employee deleted successfully!";
            }
            else
            {
                TempData["Message"] = "Error while deleting Employee!";
            }
            return RedirectToAction("Index");
        }

        #endregion

        #region Badge Edit/Delete
        public IActionResult BadgeEdit(string id)
        {
            ObjectId oId = new ObjectId(id);

            Badge badge = badgeCollection.Find(e => e.BadgeId
        == oId).FirstOrDefault();
            return View(badge);
        }

      
        [HttpPost]
        public IActionResult BadgeEdit(string id, Badge badge)
        {


            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;

                ObjectId oId = new ObjectId(id);

                Badge badges = badgeCollection.Find(e => e.BadgeId
            == oId).FirstOrDefault();

                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"images/badges");
                    var extenstion = Path.GetExtension(files[0].FileName);

                    if (badge.ImageUrl != null)
                    {
                        //this is an edit and we need to remove old image
                        var imagePath = Path.Combine(webRootPath, badge.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    badge.ImageUrl = @"\images\badges\" + fileName + extenstion;
                }
                else
                {
                    //update when they do not change the image
                    if (badge.BadgeId != null)
                    {

                        badge.ImageUrl= badges.ImageUrl;
                    }
                }

                var filter = Builders<Badge>.Filter.Eq("BadgeId", oId);
                var updateDef = Builders<Badge>.Update.
            Set("BadgeName", badge.BadgeName);
                updateDef = updateDef.Set("ImageUrl", badge.ImageUrl);
                updateDef = updateDef.Set("Description", badge.Description);
                updateDef = updateDef.Set("ExpiryDate", badge.ExpiryDate);
                
                var result = badgeCollection.UpdateOne(filter, updateDef);

                //if (result.IsAcknowledged)
                //{
                //    ViewBag.Message = "Issuer updated successfully!";
                //}
                //else
                //{
                //    ViewBag.Message = "Error while updating Issuer!";
                //}
                return RedirectToAction("Index");
            }

            return View();

        }


        public IActionResult BadgeDelete(string id)
        {
            ObjectId oId = new ObjectId(id);
            var badge = badgeCollection.DeleteOne<Badge>(e => e.BadgeId == oId);
           

            if (badge.IsAcknowledged)
            {
                TempData["Message"] = "Employee deleted successfully!";
            }
            else
            {
                TempData["Message"] = "Error while deleting Employee!";
            }
            return RedirectToAction("Index");
        }

        #endregion
        public IActionResult Award(string id)
        {
            ObjectId oid = new ObjectId(id);
            Badge badge = badgeCollection.Find(e => e.BadgeId
        == oid).FirstOrDefault();
            AwardBadge awardBadge = new AwardBadge()
            {
                id=badge.BadgeId.ToString()
            };

            return View(awardBadge);
        }
        [HttpPost]
        public async Task<IActionResult> Award(string id,AwardBadge awardBadge)
        {
            if (ModelState.IsValid)
            {
                ObjectId oid = new ObjectId(id);
                Badge badge = badgeCollection.Find(e => e.BadgeId
          == oid).FirstOrDefault();

                ObjectId issuerId = badge.IssuerId;
                Issuers issuer = collection.Find(e => e.Id
          == issuerId).FirstOrDefault();
                DateTime d = DateTime.Now;
                Badge_User_Collection buc = new Badge_User_Collection()
                {
                 BadgeId=badge.BadgeId,
                 RecipientName=awardBadge.Badge_User_Collection.RecipientName,
                    RecipientEmail = awardBadge.Badge_User_Collection.RecipientEmail,
                    IssuedDate = d,
                    ExpirationDate=badge.ExpiryDate
                };
                badgeUserCollection.InsertOne(buc);

                  await _emailSender.SendEmailAsync(buc.RecipientEmail,"Congratulation, you earned a Badge",

                      $"<div class='p-6 m-3 border rounded row' style='background-color:beige'><div class='col-3'></div><div class='col-6' style='background-color:white'><div class='row text-center m-2'><h3 style = 'color:blueviolet' >Congratulation, you earned a badge</h3></div><hr /><div class='text-center'><img src = 'https://dab1nmslvvntp.cloudfront.net/wp-content/uploads/2014/11/1415490092badge.png' width='200px' height='200px' alt='image' /></div><hr /><br /><div class='row text-left'><h4 class='m-2'>{HtmlEncoder.Default.Encode(badge.BadgeName)}</h4></div><div class='row text-left'><p class='m-2'>{HtmlEncoder.Default.Encode(badge.Description)}</p></div><hr /><hr /><div class='row text-left m-2'><p>Issued by :</p></div><div class='row text-left m-2'><p><b>{HtmlEncoder.Default.Encode(issuer.Name)}</b></p></div><hr /><div class='text-center'><a class='btn btn-secondary' href='https://localhost:44326/'>Create Account</a></div><br /><br/></div><div class='col-3'></div></div>"
                    );
                return RedirectToAction("Index");

            }

            return View();
        }
    }
}*/