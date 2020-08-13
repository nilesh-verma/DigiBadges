using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DigiBadges.DataAccess;
using DigiBadges.DataAccess.Repository;
using DigiBadges.DataAccess.ViewModels;
using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
    public class BadgeController : Controller
    {
        private readonly Repository<DigiBadges.DataAccess.Badge> _badge;
        private readonly IWebHostEnvironment _hostEnvironment;
        public IEnumerable<DigiBadges.DataAccess.Badge> badge;
        private ISolrOperations<SolrBadgeModel> _solr;
        private IMongoCollection<DigiBadges.Models.Badge> badgeCollection;
        private MongoDbSetting _mongoDbOptions { get; set; }
        public BadgeController(IWebHostEnvironment hostEnvironment, IOptions<MongoDbSetting> mongoDbOptions, Repository<DataAccess.Badge> badge, ISolrOperations<SolrBadgeModel> solr)
        {
            _badge = badge;
            _solr = solr;
            _mongoDbOptions = mongoDbOptions.Value;
            _hostEnvironment = hostEnvironment;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            this.badgeCollection = db.GetCollection<DigiBadges.Models.Badge>("Badges");
        }
        public IActionResult Index()
        {
           
            try
            {
                badge = _badge.AsQueryable().ToList();
                //var bdg = badgeCollection.Find(FilterDefinition<DigiBadges.Models.Badge>.Empty).ToList();

                //var results = _solr.Query(SolrQuery.All);

                //if(results != null)
                //{
                //    bool flag = false;
                //    for(int badg=0;badg < bdg.Count(); badg++)
                //    {
                //        flag = false;
                //        for(int solrBadge=0; solrBadge < results.Count; solrBadge++)
                //        {
                //            if(bdg[badg].BadgeId.ToString() == results[solrBadge].BadgeId)
                //            {
                //                flag = true;
                //                break;
                //            }


                //        }
                //        if(flag == false)
                //        {
                //            SolrBadgeModel sbm = new SolrBadgeModel(bdg[badg]);
                            
                //            //_solr.Add(sbm);
                //            //_solr.Commit();
                //        }

                //    }
                //}

            }
            catch(Exception e)
            {
                TempData["ExceptionMessage"] = "Please try again";
                return View();
            }
            BadgeVM badgeVM = new BadgeVM()
            {
                Badge=badge
            };

            return View(badgeVM);
        }
     
        
        [HttpGet]
        public IActionResult Search(string id)
       {
            try
            {
                string query = "Name:" + "*"+id+"*";
                // id = HttpContext.Request.Query["term"].ToString();
                var results = _solr.Query(query);

                if (id != null)
                {
                    var names = results.Where(m => m.BadgeName.Contains(id)).Select(m => m.BadgeName).Take(5).ToList();
                    return Ok(names);
                }
                else if(results == null)
                {
                    string[] s = new[] { "No match found" };
                    return Ok(s.ToList());
                }
                else
                {
                    //string[] s = new []{""};
                    return Ok();
                }
            }
            catch(Exception e)
            {
                return BadRequest();
            }
        }




        public IActionResult Create()
        {
            List<string> Num = new List<string>() { "Days", "Week", "Month", "Years" };
            ViewBag.Number = new SelectList(Num);

            return View();
        }

        [HttpPost]
        public IActionResult Create(string Select, DataAccess.Badge badge)
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
                    case "Week":
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
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"Images\badges");
                    var extenstion = Path.GetExtension(files[0].FileName);


                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }



                    badge.ImageUrl = @"\images\badges\" + fileName + extenstion;

                }

                DataAccess.Badge b = new DataAccess.Badge()
                {

                    BadgeName = badge.BadgeName,
                    ImageUrl = badge.ImageUrl,
                    ExpiryDuration = badge.ExpiryDuration,
                    EarningCriteriaDescription = badge.EarningCriteriaDescription,
                    CreatedDate = DateTime.Now,
                    FacebookId = badge.FacebookId
                };
                _badge.InsertOne(b);
                return RedirectToAction("Index");

            }

            return View();
        }

        #region Badge Edit/Delete
        public IActionResult BadgeEdit(string id)
        {
           

            var badge = _badge.FindById(id);
            return View(badge);
        }


        [HttpPost]
        public IActionResult BadgeEdit(string id, DataAccess.Badge badge)
        {


            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;

                ObjectId oId = new ObjectId(id);
                var badges = _badge.FindById(id);
                badge.Id = new ObjectId(id);

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
                    if (badge.Id != null)
                    {

                        badge.ImageUrl = badges.ImageUrl;
                    }
                }

                

                _badge.ReplaceOne(badge);

                
                return RedirectToAction("Index");
            }

            return View();

        }


       

        
        public IActionResult BadgeDelete(string id)
        {
            _badge.DeleteById(id);
            return RedirectToAction("Index");
        }
        #endregion
    }
}