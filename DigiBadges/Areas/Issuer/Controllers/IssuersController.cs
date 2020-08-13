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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SolrNet;

namespace DigiBadges.Areas.Issuer.Controllers
{
    [Area(AppUtility.IssuerRole)]    
    [Authorize(Roles = AppUtility.IssuerRole)]
    public class IssuersController : Controller
    {
        #region PROPERTIES
        private IMongoCollection<Issuers> collection;
        private IMongoCollection<Badge> badgeCollection;
        private readonly ILogger<IssuersController> _logger;
        private IMongoCollection<EarnerBadgeDetails> earnerBadgeDetails;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IEmailSender _emailSender;
        public Repository<DigiBadges.DataAccess.EarnerBadgeDetails> _ed;
        private IMongoCollection<Staff> Staff;
        public Repository<DigiBadges.DataAccess.Badge> _b;
        public Repository<DigiBadges.DataAccess.Issuers> _i;
        public Repository<DigiBadges.DataAccess.Users> _u;
        private IMongoCollection<Users> Users;
        private ISolrOperations<SolrBadgeModel> _solr;

        private MongoDbSetting _mongoDbOptions { get; set; }
        #endregion

        #region  CONSTRUCTOR

        public IssuersController(ILogger<IssuersController> logger, IWebHostEnvironment hostEnvironment, IOptions<MongoDbSetting> mongoDbOptions,
            IEmailSender emailSender, ISolrOperations<SolrBadgeModel> solr, Repository<DigiBadges.DataAccess.EarnerBadgeDetails> ed, Repository<DigiBadges.DataAccess.Badge> b, Repository<DigiBadges.DataAccess.Users> u, Repository<DigiBadges.DataAccess.Issuers> i)
        {
            _mongoDbOptions = mongoDbOptions.Value;
            _b = b;
            _logger = logger;
            _solr = solr;
            _i = i;
            _ed = ed;
            _u = u;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.collection = db.GetCollection<Issuers>("Issuers");
            this.badgeCollection = db.GetCollection<Badge>("Badges");
            Users = db.GetCollection<Users>("Users");

            this.earnerBadgeDetails = db.GetCollection<EarnerBadgeDetails>("EarnerBadgeDetails");
            Staff = db.GetCollection<Staff>("Staff");
            _hostEnvironment = hostEnvironment;
            _emailSender = emailSender;
        }

        #endregion

        #region GET ISSUER AND THEIR BADGES

        public IActionResult Issuers()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;  //claim[3].Value;
            var issuer = collection.Find(FilterDefinition<Issuers>.Empty).ToList();
            var userSpecificIssuer = issuer.Where(e => e.UserId == new ObjectId(userid)).FirstOrDefault();
            ObjectId oId = userSpecificIssuer.IssuerId;
            Badge b = badgeCollection.Find(e => e.IssuerId
        == oId).FirstOrDefault();
            int count = 0;
            Issuers issuers = collection.Find(e => e.IssuerId
        == oId).FirstOrDefault();
            IEnumerable<Badge> badge = badgeCollection.Find(e => e.IssuerId == oId).ToList();

            if (b == null)
            {
                count = 0;
            }
            else
            {
                #region test
                var usr = _u.FindById(userid.ToString());
                var isser = _i.AsQueryable().ToList();
                var actualIssuer = isser.Where(e => e.UserId == usr.Id).FirstOrDefault();
                var bdg = _b.AsQueryable().ToList();
                var totalBadg = bdg.Where(e => e.IssuerId == actualIssuer.Id).ToList();

                for(int l = 0; l < totalBadg.Count; l++)
                {
                    var earnerBadge = earnerBadgeDetails.Find(e => e.BadgeId ==new ObjectId( totalBadg[l].Id.ToString())).ToList();
                    count =count+ earnerBadge.Count();
                }

                #endregion



                //IEnumerable<EarnerBadgeDetails> earnerBadge = earnerBadgeDetails.Find(e => e.BadgeId == b.BadgeId).ToList();
                //count = earnerBadge.Count();
            }
            IssuerBadge issuerBadge = new IssuerBadge()
            {
                Id = oId.ToString(),
                issuer = issuers,
                badge = badge,
                Badge_Count = count


            };
            return View(issuerBadge);
        }

        #endregion

        #region CREATE AND POST BADGE
        public IActionResult Badge()
        {
            
            List<string> Num = new List<string>() { "Days", "Weeks", "Months", "Years" };
            ViewBag.Number = new SelectList(Num);
            return View();
        }

        [HttpPost]
        public IActionResult Badge(string Select, Badge issuerBadge)
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;            //claim[3].Value;
            var issuer = collection.Find(FilterDefinition<Issuers>.Empty).ToList();
            var userSpecificIssuer = issuer.Where(e => e.UserId == new ObjectId(userid)).FirstOrDefault();
            ObjectId iid = userSpecificIssuer.IssuerId;

            if (ModelState.IsValid)
            {
                
                    double d = Math.Ceiling(issuerBadge.ExpiryDuration);
                    switch (Select)
                    {
                        case "Days":
                            {
                                issuerBadge.ExpiryDuration = d;


                            }
                            break;
                        case "Weeks":
                            {
                                issuerBadge.ExpiryDuration = d * 7;


                            }
                            break;
                        case "Months":
                            {
                                issuerBadge.ExpiryDuration = d * 30;
                            }
                            break;

                        case "Years":
                            {
                                issuerBadge.ExpiryDuration = d * 365;
                            }
                            break;

                    }

                    string webRootPath = _hostEnvironment.WebRootPath;
                    var files = HttpContext.Request.Form.Files;
                    if (files.Count > 0)
                    {
                        string fileName = Guid.NewGuid().ToString();
                        var uploads = Path.Combine(webRootPath, @"badges");
                    
                    var extenstion = Path.GetExtension(files[0].FileName);
                   

                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                        {
                            files[0].CopyTo(filesStreams);
                       
                    }



                        issuerBadge.ImageUrl = @"\badges\" + fileName + extenstion;
                   
                }

                    DateTime today = DateTime.Now;

                    Badge b = new Badge()
                    {
                        IssuerId = userSpecificIssuer.IssuerId,
                        BadgeName = issuerBadge.BadgeName,
                        ImageUrl = issuerBadge.ImageUrl,
                        ExpiryDuration = issuerBadge.ExpiryDuration,
                        EarningCriteriaDescription = issuerBadge.EarningCriteriaDescription,
                        CreatedDate = today,
                        FacebookId = issuerBadge.FacebookId,
                        CreatedBy = userSpecificIssuer.Name
                    };
                    badgeCollection.InsertOne(b);
                _logger.LogError("225");
                SolrBadgeModel solrBadg = new SolrBadgeModel(b.BadgeName,b.BadgeId.ToString());
                //solrBadg.BadgeId = b.BadgeId.ToString(); 
                _solr.Add(solrBadg);
                _solr.Commit();
                return RedirectToAction("Issuers"); 

            }

            return View();
        }

        #endregion

        #region VIEW BADGE WITH AWARDED BADGE
        public IActionResult ViewBadge(string id, int productPage = 1, string recipientEmail = null, string OID = null)
        {
            int PageSize = 2;
            ObjectId oId;
            if (id == null)
            {
                oId = new ObjectId(TempData["OID"].ToString());
                TempData["OID"] = oId.ToString();
            }

            else
            {
                oId = new ObjectId(id);
                TempData["OID"] = id;
            }

            StringBuilder param = new StringBuilder();

            param.Append("/Issuer/Issuers/ViewBadge?productPage=:");
            param.Append("&recipientEmail=");
            if (recipientEmail != null)
            {
                param.Append(recipientEmail);
            }




            Badge badge = badgeCollection.Find(e => e.BadgeId
        == oId).FirstOrDefault();
            ObjectId iid = badge.IssuerId;
            Issuers issuers = collection.Find(e => e.IssuerId
        == iid).FirstOrDefault();

            IEnumerable<EarnerBadgeDetails> badgeEarner = earnerBadgeDetails.Find(e => e.BadgeId
          == oId).ToList();
           
            if (badge != null)
            {
                TempData["badgetest"] = badge.FacebookId;
            }

            var earner = _ed.AsQueryable().ToList();
            var badgeInEarner = earner.Where(e => e.BadgeId == oId).ToList();
            bool earnerCheck = false;
            if (badgeInEarner.Count > 0)
            {
                earnerCheck = true;
            }
            else
            {
                earnerCheck = false;
            }

            IssuerBadge i = new IssuerBadge()
            {
                badge1 = badge,
                issuer = issuers,
                EarnerBadgeDetails = badgeEarner,
                IsBadgeAvailableInEarner = earnerCheck

            };



            if (recipientEmail != null)
            {
                i.EarnerBadgeDetails = i.EarnerBadgeDetails.Where(a => a.RecipientEmail.ToLower().Contains(recipientEmail.ToLower())).ToList();
            }
            var count = i.EarnerBadgeDetails.Count();

            i.EarnerBadgeDetails = i.EarnerBadgeDetails.OrderBy(p => p.AwardedDate)
                .Skip((productPage - 1) * PageSize)
                .Take(PageSize).ToList();


            i.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItems = count,
                urlParam = param.ToString(),


            };
            return View(i);
        }

        #endregion

        #region Badge Edit/Delete
        public IActionResult BadgeEdit( string id)
        {
            ObjectId oId = new ObjectId(id);
            List<string> Num = new List<string>() { "Days", "Weeks", "Months", "Years" };
            ViewBag.Number = new SelectList(Num);
            Badge badge = badgeCollection.Find(e => e.BadgeId
        == oId).FirstOrDefault();
            return View(badge);
        }


        [HttpPost]
        public IActionResult BadgeEdit(string Select, string id, Badge badge)
        {


            if (ModelState.IsValid)
            {
               

                    double d = Math.Ceiling(badge.ExpiryDuration);
                    switch (Select)
                    {
                        case "Days":
                            {
                                badge.ExpiryDuration = d;


                            }
                            break;
                        case "Weeks":
                            {
                                badge.ExpiryDuration = d * 7;


                            }
                            break;
                        case "Months":
                            {
                                badge.ExpiryDuration = d * 30;
                            }
                            break;

                        case "Years":
                            {
                                badge.ExpiryDuration = d * 365;
                            }
                            break;

                    }


                    string webRootPath = _hostEnvironment.WebRootPath;
                    var files = HttpContext.Request.Form.Files;

                    ObjectId oId = new ObjectId(id);

                    Badge badges = badgeCollection.Find(e => e.BadgeId
                == oId).FirstOrDefault();

                    if (files.Count > 0)
                    {
                        string fileName = Guid.NewGuid().ToString();
                        var uploads = Path.Combine(webRootPath, @"badges");
                        var extenstion = Path.GetExtension(files[0].FileName);

                        if (badges.ImageUrl != null)
                        {
                            //this is an edit and we need to remove old image
                            var imagePath = Path.Combine(webRootPath, badges.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }
                        using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                        {
                            files[0].CopyTo(filesStreams);
                        }
                        badge.ImageUrl = @"\badges\" + fileName + extenstion;
                    }
                    else
                    {
                        //update when they do not change the image
                        if (badge.BadgeId != null)
                        {

                            badge.ImageUrl = badges.ImageUrl;
                        }
                    }

                    var filter = Builders<Badge>.Filter.Eq("BadgeId", oId);
                    var updateDef = Builders<Badge>.Update.
                Set("BadgeName", badge.BadgeName);
                    updateDef = updateDef.Set("ImageUrl", badge.ImageUrl);
                    updateDef = updateDef.Set("EarningCriteriaDescription", badge.EarningCriteriaDescription);
                    updateDef = updateDef.Set("ExpiryDuration", badge.ExpiryDuration);

                    var result = badgeCollection.UpdateOne(filter, updateDef);      //to update
                Badge badg = badgeCollection.Find(e => e.BadgeId
        == oId).FirstOrDefault();
                SolrBadgeModel solrBadg = new SolrBadgeModel(badg.BadgeName,badg.BadgeId.ToString());
                //solrBadg.BadgeId = b.BadgeId.ToString(); 
                _solr.Add(solrBadg);
                _solr.Commit();
                return RedirectToAction("ViewBadge",new { id=id});
            }

            return View();

        }


        public IActionResult BadgeDelete(string id)
        {
            ObjectId oId = new ObjectId(id);
            
            var earnerBadge= earnerBadgeDetails.DeleteMany<EarnerBadgeDetails>(e => e.BadgeId == oId);
            Badge badg = badgeCollection.Find(e => e.BadgeId
        == oId).FirstOrDefault();

            SolrBadgeModel sbdg = new SolrBadgeModel(badg.BadgeName,badg.BadgeId.ToString());
            _solr.Delete(sbdg);
            _solr.Commit();
            var badge = badgeCollection.DeleteOne<Badge>(e => e.BadgeId == oId);         //To delete
              

            if (badge.IsAcknowledged)
            {
                TempData["Message"] = "Employee deleted successfully!";
            }
            else
            {
                TempData["Message"] = "Error while deleting Employee!";
            }
            return RedirectToAction("Issuers");
        }

        #endregion

        #region AWARD BADGE
        public IActionResult Award(string id)
        {
            ObjectId oid = new ObjectId(id);
            Badge badge = badgeCollection.Find(e => e.BadgeId
        == oid).FirstOrDefault();
            AwardBadge awardBadge = new AwardBadge()
            {
                id = badge.BadgeId.ToString()
            };

            return View(awardBadge);
        }
        [HttpPost]
        public async Task<IActionResult> Award(string id, AwardBadge awardBadge)
        {
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
                var claim = claimsIdentity.Claims.ToArray();
                var email = claim[1].Value;
                var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;
                ObjectId oid = new ObjectId(id);
                Badge badge = badgeCollection.Find(e => e.BadgeId
          == oid).FirstOrDefault();

                ObjectId issuerId = badge.IssuerId;
                Issuers issuer = collection.Find(e => e.IssuerId
          == issuerId).FirstOrDefault();
                DateTime d = DateTime.Now;
                DateTime d1 = DateTime.Today.AddDays(badge.ExpiryDuration);
                EarnerBadgeDetails buc = new EarnerBadgeDetails()
                {
                    BadgeId = badge.BadgeId,
                    RecipientName = awardBadge.EarnerBadgeDetails.RecipientName,
                    RecipientEmail = awardBadge.EarnerBadgeDetails.RecipientEmail,
                    AwardedDate = d,
                    ExpirationDate = d1,
                    UserId = new ObjectId(userid.ToString())
                };
                earnerBadgeDetails.InsertOne(buc);

                /*await _emailSender.SendEmailAsync(buc.RecipientEmail, "Congratulation, you earned a Badge",

                    $"<div class='p-6 m-3 border rounded row' style='background-color:beige'><div class='col-3'></div><div class='col-6' style='background-color:white'><div class='row text-center m-2'><h3 style = 'color:blueviolet' >Congratulation, you earned a badge</h3></div><hr /><div class='text-center'><img src = 'https://dab1nmslvvntp.cloudfront.net/wp-content/uploads/2014/11/1415490092badge.png' width='200px' height='200px' alt='image' /></div><hr /><br /><div class='row text-left'><h4 class='m-2'>{HtmlEncoder.Default.Encode(badge.BadgeName)}</h4></div><div class='row text-left'><p class='m-2'>{HtmlEncoder.Default.Encode(badge.EarningCriteriaDescription)}</p></div><hr /><hr /><div class='row text-left m-2'><p>Issued by :</p></div><div class='row text-left m-2'><p><b>{HtmlEncoder.Default.Encode(issuer.Name)}</b></p></div><hr /><div class='text-center'><a class='btn btn-secondary' href='https://localhost:44326/'>Create Account</a></div><br /><br/></div><div class='col-3'></div></div>"
                  );*/
                return RedirectToAction("ViewBadge", new { id = badge.BadgeId });

            }

            return View();
        }
        #endregion
    }

}