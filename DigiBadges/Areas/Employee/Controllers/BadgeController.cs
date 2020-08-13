using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DigiBadges.DataAccess.Repository;
using DigiBadges.Models;
using DigiBadges.Models.ViewModels;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Ubiety.Dns.Core;

namespace DigiBadges.Areas.Employee.Controllers
{
    [Authorize(Roles = AppUtility.EarnerRole)]
    [Area("Employee")]

    public class BadgeController : Controller
    {
        #region properties

        private readonly IDistributedCache _cache;
        private IMongoCollection<EarnerBadgeDetails> EarnerBadgeDetails;
        private IMongoCollection<BadgeCollections> BadgeCollections;
        private IMongoCollection<Badge> Badge;
        private IMongoCollection<Issuers> Issuers;
        private readonly Repository<DigiBadges.DataAccess.Badge> _badges;
        private readonly Repository<DigiBadges.DataAccess.EarnerBadgeDetails> _ed;
        private MongoDbSetting _mongoDbOptions { get; set; }

        private readonly IWebHostEnvironment _hostEnvironment;
        #endregion

        #region Constructor
        public BadgeController(IDistributedCache cache, IWebHostEnvironment hostEnvironment, Repository<DigiBadges.DataAccess.EarnerBadgeDetails> ed, Repository<DigiBadges.DataAccess.Badge> badges, IOptions<MongoDbSetting> mongoDbOptions)
        {
            _hostEnvironment = hostEnvironment;
            _badges = badges;
            _cache = cache;
            _ed = ed;
            _mongoDbOptions = mongoDbOptions.Value;
            var client = new MongoClient(_mongoDbOptions.ConnectionString);
            IMongoDatabase db = client.GetDatabase(_mongoDbOptions.Database);
            EarnerBadgeDetails = db.GetCollection<EarnerBadgeDetails>("EarnerBadgeDetails");
            BadgeCollections = db.GetCollection<BadgeCollections>("BadgeCollections");
            Issuers = db.GetCollection<Issuers>("Issuers");
            Badge = db.GetCollection<Badge>("Badges");
        }
        #endregion


        #region BackPackIndexPage

        public void BadgeExpiryCheck(string email)
        {

            var badgeOfEarnerEmail = EarnerBadgeDetails.Find(e => e.RecipientEmail == email).ToList();
            var earnerbdg = _ed.AsQueryable().ToList();
            var earnerBadge = earnerbdg.Where(e => e.RecipientEmail == email).ToList();

            
            if (earnerBadge != null)
            {
              for(int i = 0; i < earnerBadge.Count; i++)
                {
                  DigiBadges.DataAccess.EarnerBadgeDetails ebd = earnerBadge[i];
                    //DateTime date = DateTime.Today.AddDays(3);
                    DateTime date = DateTime.Now;
                    int res = DateTime.Compare(date, ebd.ExpirationDate);
                    if (res > 0)
                    {
                        ebd.IsExpired = true;
                        _ed.ReplaceOne(ebd);
                    }
                }  

            }
        }
        public async Task<IActionResult> Index()
        {
            //get the current earner  email and id
            
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;

            CollectionView collectionView = new CollectionView(); //initialize the viewmodel
            BadgeExpiryCheck(email);
             
            //get the info of logged in earner
            var earnerBadges = EarnerBadgeDetails.Find(e => e.RecipientEmail == email ).ToList();
            List<BackPack> list = new List<BackPack>();
            #region Redis
            var earnerId =await  _cache.GetSearchResultAsync(userid);
            if (earnerId != null)
            {
                collectionView.BackPacks = earnerId;
            }

            else
            {

                if (earnerBadges.Count() != null)
                {
                    List<int> add_list = new List<int>();
                    for (var k = 0; k < earnerBadges.Count(); k++)
                    {
                        var badgeid = earnerBadges[k].BadgeId;
                        var badgedetailsnew = Badge.Find(e => e.BadgeId == earnerBadges[k].BadgeId).FirstOrDefault();
                        var issuerids = badgedetailsnew.IssuerId;
                        var issuerdetailsnew = Issuers.Find(e => e.IssuerId == issuerids).FirstOrDefault();
                        var facebookid = Badge.Find(e => e.BadgeId == badgeid).FirstOrDefault().FacebookId;

                        BackPack b = new BackPack()
                        {
                            BadgeName = badgedetailsnew.BadgeName,
                            earningDescription = badgedetailsnew.EarningCriteriaDescription,
                            ImageUrl = badgedetailsnew.ImageUrl,
                            IssuerName = issuerdetailsnew.Name,
                            badgeid = badgeid.ToString(),
                            FacebookId = facebookid,
                            IsExpired = earnerBadges[k].IsExpired
                        };

                        list.Add(b);
                        DateTime expireDuration = earnerBadges[k].ExpirationDate.Date;
                        DateTime dtNow = DateTime.Now;
                        TimeSpan result = expireDuration.Subtract(dtNow);
                        int seconds = Convert.ToInt32(result.TotalSeconds);
                        add_list.Add(seconds);
                        

                    }
                    int[] secondsForTotalBadge = add_list.ToArray();

                    await _cache.AddSearchResultsAsync(userid, list, secondsForTotalBadge.Min());
                    collectionView.BackPacks = list;

                }
            }
            #endregion
            collectionView.BadgeCollections = BadgeCollections.Find(e => e.UserId == new ObjectId(userid)).ToList();

            #region showing badge in collection
            var totalBadgeCollection= BadgeCollections.Find(e => e.UserId == new ObjectId(userid)).ToList();
            var totalBadgeImage = new List<BadgeImage>();
            if (totalBadgeCollection != null)
            {
                for (int badgeColl = 0; badgeColl < totalBadgeCollection.Count; badgeColl++)
                {

                    string[] b = totalBadgeCollection[badgeColl].BadgeId;
                    if (b != null)
                    {
                        for (var n = 0; n < b.Length; n++)
                        {
                            DigiBadges.DataAccess.Badge bdg = _badges.FindById(b[n]);

                            if (b[n].ToString() == bdg.Id.ToString())
                            {
                                BadgeImage badgeImage = new BadgeImage()
                                {
                                    ID = b[n].ToString(),
                                    ImageUrl = bdg.ImageUrl
                                };
                                totalBadgeImage.Add(badgeImage);

                            }
                        }
                    }
                }
            }
            collectionView.BI = totalBadgeImage;
            #endregion


            return View(collectionView);
        }
        #endregion




        #region CreateCollection
        public IActionResult CreateCollection()
        {

            return View();
        }


        [HttpPost]
        public IActionResult CreateCollection(BadgeCollections createCollection)
        {
            
            var UserId = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value; ;
            createCollection.UserId = new ObjectId(UserId);
            this.BadgeCollections.InsertOne(createCollection);
            ObjectId id = createCollection.CollectionId;
            return RedirectToAction("Collections", "Badge", new { id = createCollection.CollectionId });
        }
        #endregion


        #region EditCollection
        [HttpGet]
        public IActionResult EditCollection(string id)
        {
            ObjectId oId = new ObjectId(id);

            BadgeCollections badge = BadgeCollections.Find(e => e.CollectionId == oId).FirstOrDefault();
            return View(badge);
        }

        [HttpPost]
        public IActionResult EditCollection(string id, BadgeCollections badgeCollections)
        {

            if (ModelState.IsValid)
            {

                ObjectId oId = new ObjectId(id);
                BadgeCollections badges = BadgeCollections.Find(e => e.CollectionId == oId).FirstOrDefault();
                var filter = Builders<BadgeCollections>.Filter.Eq("_id", oId);
                var updateDef = Builders<BadgeCollections>.Update.
                Set("CollectionName", badgeCollections.CollectionName);
                updateDef = updateDef.Set("CollectionDescription", badgeCollections.CollectionDescription);
                BadgeCollections.UpdateOne(filter, updateDef);
            }
            return RedirectToAction("Index");

        }
        #endregion

        #region DeleteCollection

        public IActionResult DeleteCollection(string id)
        {
            ObjectId oId = new ObjectId(id);
            var badge = BadgeCollections.DeleteOne<BadgeCollections>(e => e.CollectionId == oId);

            return RedirectToAction("Index");

        }
        #endregion

        public IActionResult CollectionBadgeDelete(string deleteid)
        {
            CollectionView collectionView = new CollectionView();
            //get loggedin user
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == "UserId").Value;

            //get detail of current earner
            var earnerbadge = EarnerBadgeDetails.Find(e => e.RecipientEmail == email).ToList();

            if (deleteid != null)
            {
                var badgecollections = BadgeCollections.Find(e => e.CollectionId == new ObjectId(TempData["collid"].ToString())).FirstOrDefault();

                string[] arraysid = badgecollections.BadgeId;

                var mylist = new List<string>();
                List<string> list = new List<string>(arraysid);

                if (arraysid.Length > 0)
                {
                    for (var a = 0; a < arraysid.Length; a++)
                    {
                        if (arraysid.Length == 1)
                        {

                            if(arraysid[a] == deleteid)
                            {
                                string[] ids = null;
                                // update the array of badgeids and remove badges
                                var BadgeCollectionsfilter = Builders<BadgeCollections>.Filter.Eq("CollectionId", new ObjectId(TempData["collid"].ToString()));
                                var BadgeCollectionsupdateDef = Builders<BadgeCollections>.Update.Set("BadgeId", ids);
                                BadgeCollections.UpdateOne(BadgeCollectionsfilter, BadgeCollectionsupdateDef);
                            }
                            
                        }
                        else
                        {
                            var indexarray = arraysid.IndexOf(deleteid);
                            if(indexarray >=0)
                            {
                                list.RemoveAt(indexarray);
                                arraysid = list.ToArray();
                                // update the array of badgeids and remove badges
                                var filter = Builders<BadgeCollections>.Filter.Eq("CollectionId", new ObjectId(TempData["collid"].ToString()));
                                var updateDef = Builders<BadgeCollections>.Update.Set("BadgeId", arraysid);
                                BadgeCollections.UpdateOne(filter, updateDef);
                            }
                           
                        }


                    }
                }


            }
            return RedirectToAction("Collections", new { id = TempData["collid"].ToString() });

        }



        #region viewCollections
        public IActionResult Collections(string id, string[] values)
        {
            CollectionView collectionView = new CollectionView();

           //get the logged user
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == "UserId").Value;
            var earnerbadge = EarnerBadgeDetails.Find(e => e.RecipientEmail == email).ToList();


        //check collection id notequal to null
            if (id != null)
            {
                List<BackPack> list = new List<BackPack>();
                var ids = id;
                TempData["collid"] = ids;
                var badgecollection = BadgeCollections.Find(FilterDefinition<BadgeCollections>.Empty).ToList();
                var badgecollections = badgecollection.Where(e => e.CollectionId == new ObjectId(id)).FirstOrDefault();
                var badgecollectionslist = BadgeCollections.Find(e => e.CollectionId == new ObjectId(id)).ToList();
                List<EarnerBadgeDetails> sb = new List<EarnerBadgeDetails>();
                if (badgecollections.BadgeId != null)
                {
                    foreach (EarnerBadgeDetails earBadge in earnerbadge)
                    {
                        if (badgecollections.BadgeId.Contains(earBadge.BadgeId.ToString()))
                            sb.Add(earBadge);

                    }
                    if (sb.Count() != null)
                    {

                        for (var k = 0; k < sb.Count(); k++)
                        {

                            var badgedetailsnew = Badge.Find(e => e.BadgeId == sb[k].BadgeId).FirstOrDefault();
                            var badgedetailsnew123 = Badge.Find(e => e.BadgeId == sb[k].BadgeId).ToList();

                            var issuerids = badgedetailsnew.IssuerId;
                            var issuerdetailsnew = Issuers.Find(e => e.IssuerId == issuerids).FirstOrDefault();

                            BackPack b = new BackPack()
                            {
                                BadgeName = badgedetailsnew.BadgeName,
                                earningDescription = badgedetailsnew.EarningCriteriaDescription,
                                ImageUrl = badgedetailsnew.ImageUrl,
                                IssuerName = issuerdetailsnew.Name,
                                badgeid = badgedetailsnew123.FirstOrDefault().BadgeId.ToString(),
                                IsExpired=sb[k].IsExpired


                            };

                            list.Add(b);
                        }
                        collectionView.BackPacks = list;

                    }

                    collectionView.BadgeCollections = badgecollectionslist;
                    return View(collectionView);

                }
                else
                {
                    collectionView.BadgeCollections = badgecollectionslist;
                    return View(collectionView);

                }

            }
            //check checked badges notequal to null
            if (values.Length != null)
            {
                var badgecollections = BadgeCollections.Find(e => e.CollectionId == new ObjectId(TempData["collid"].ToString())).FirstOrDefault();

                var mylist = new List<string>();
                 mylist.AddRange(values);
                if (badgecollections.BadgeId != null)
                {
                    mylist.AddRange(badgecollections.BadgeId);
                }
                string[] totalbadges = mylist.ToArray();
                var filter = Builders<BadgeCollections>.Filter.Eq("_id", new ObjectId(TempData["collid"].ToString()));
                var updateDef = Builders<BadgeCollections>.Update.Set("BadgeId", totalbadges);
                BadgeCollections.UpdateOne(filter, updateDef);
                var badgecollectionslist1 = BadgeCollections.Find(e => e.CollectionId == new ObjectId(TempData["collid"].ToString())).ToList();
                collectionView.BadgeCollections = badgecollectionslist1;

                return RedirectToAction("Collections" ,new { id =TempData["collid"].ToString() });


            }

            return View();

        }

        #endregion




        #region addedBadgesincollection
        public IActionResult _PopupPartial(string id)
        {
            if (id != null)
            {
                TempData["id"] = id; //current  collection id
                CollectionView collectionView = new CollectionView();

                var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email).Value;
                var userid = User.Claims.FirstOrDefault(c => c.Type == "UserId").Value;

                var badgecollections = BadgeCollections.Find(e => e.CollectionId == new ObjectId(id)).ToList();//get current collection

                var badge = BadgeCollections.Find(e => e.CollectionId == new ObjectId(id)).FirstOrDefault();//get inserted badges 

                var earnerbadg = EarnerBadgeDetails.Find(e => e.RecipientEmail == email).ToList(); // get total earn badges
                var earnerbadge = earnerbadg.Where(e => e.IsExpired == false).ToList();

                List<EarnerBadgeDetails> sb = new List<EarnerBadgeDetails>();
                List<BackPack> list = new List<BackPack>();

                if (badge.BadgeId != null)
                {
                    foreach (EarnerBadgeDetails earBadge in earnerbadge)
                    {

                        if (!badgecollections.FirstOrDefault().BadgeId.Contains(earBadge.BadgeId.ToString()))
                            sb.Add(earBadge);

                    }

                    if (sb.Count() != null)
                    {

                        for (var k = 0; k < sb.Count(); k++)
                        {
                            var badgeid = sb[k].BadgeId;
                            var badgedetailsnew = Badge.Find(e => e.BadgeId == sb[k].BadgeId).FirstOrDefault();

                            var issuerids = badgedetailsnew.IssuerId;
                            var issuerdetailsnew = Issuers.Find(e => e.IssuerId == issuerids).FirstOrDefault();
                            BackPack b = new BackPack()
                            {
                                BadgeName = badgedetailsnew.BadgeName,
                                earningDescription = badgedetailsnew.EarningCriteriaDescription,
                                ImageUrl = badgedetailsnew.ImageUrl,
                                IssuerName = issuerdetailsnew.Name,
                                badgeid = badgeid.ToString(),
                            };

                            list.Add(b);
                        }
                        collectionView.BackPacks = list;

                    }
                    collectionView.BadgeCollections = badgecollections;
                    return View(collectionView);
                }
                else
                {
                    var earnerbadgedetail = EarnerBadgeDetails.Find(e => e.RecipientEmail == email).ToList();
                    var earnerbadgedetails = earnerbadgedetail.Where(e => e.IsExpired == false).ToList();

                    if (earnerbadgedetails.Count() != null)
                    {

                        for (var k = 0; k < earnerbadgedetails.Count(); k++)
                        {
                            var badgeid = earnerbadgedetails[k].BadgeId;
                            var badgedetailsnew = Badge.Find(e => e.BadgeId == earnerbadgedetails[k].BadgeId).FirstOrDefault();

                            var issuerids = badgedetailsnew.IssuerId;
                            var issuerdetailsnew = Issuers.Find(e => e.IssuerId == issuerids).FirstOrDefault();
                            BackPack b = new BackPack()
                            {
                                BadgeName = badgedetailsnew.BadgeName,
                                earningDescription = badgedetailsnew.EarningCriteriaDescription,
                                ImageUrl = badgedetailsnew.ImageUrl,
                                IssuerName = issuerdetailsnew.Name,
                                badgeid = badgeid.ToString(),
                            };

                            list.Add(b);
                        }
                        collectionView.BackPacks = list;

                    }
                    collectionView.EarnerBadgeDetails = earnerbadgedetails;
                    return View(collectionView);
                }

            }



            return View();

        }

        #endregion
    }
}





