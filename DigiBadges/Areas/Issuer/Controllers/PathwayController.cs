using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DigiBadges.Areas.Identity.Pages.Account;
using DigiBadges.DataAccess;
using DigiBadges.DataAccess.Repository;
using DigiBadges.DataAccess.ViewModels;
using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;
using InputModel = DigiBadges.DataAccess.InputModel;

namespace DigiBadges.Areas.Issuer.Controllers
{
    [Area(AppUtility.IssuerRole)]
    [Authorize(Roles = AppUtility.AdminRole + "," + AppUtility.IssuerRole)]
    public class PathwayController : Controller
    {
        private readonly Repository<Pathways> _path;
        private readonly Repository<DataAccess.Badge> _badge;
        private readonly Repository<DataAccess.Issuers> _issuer;
        private readonly Repository<DataAccess.Users> _users;
        private readonly Repository<PathwaySteps> _peopleRepository;
        private readonly IWebHostEnvironment _hostEnvironment;

        private Repository<InputModel> _reg;
        public PathwayController(Repository<Pathways> path, Repository<DataAccess.Users> users, Repository<DataAccess.Issuers> issuer, Repository<InputModel> reg, IWebHostEnvironment hostEnvironment, Repository<PathwaySteps> peopleRepository, Repository<DataAccess.Badge> badge)
        {
            _issuer = issuer;
            _users = users;
            _path = path;
            _reg = reg;
            _hostEnvironment = hostEnvironment;
            _badge = badge;
            _peopleRepository = peopleRepository;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;
            //User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;;

            var user = _users.FindById(userid.ToString());
            var issuers = _issuer.AsQueryable().ToList();
            var issuer = issuers.Where(e => e.UserId == new ObjectId(userid)).FirstOrDefault();

            var pathway = _path.AsQueryable().ToList();
            var userSpecificPathway = pathway.Where(e => e.IssuersId == issuer.Id);
            var pathwaySteps = _peopleRepository.AsQueryable().ToList();
            //PathwaySteps p = new PathwaySteps()
            //{
            //    Id = new ObjectId("5efb275c3cee0204d801e303"),

            //};
            //var enumerable = new[] { p };
            PathwayVM pathwayVM = new PathwayVM()
            {
                pathways = userSpecificPathway,
                pathwaySteps = pathwaySteps

            };
            return View(pathwayVM);
        }

        public IActionResult PathwaySteps(string id)
        {
            //var pathwaySteps = _peopleRepository.AsQueryable().Where(steps=>steps.PathwayId==id).ToList();

            //return View(pathwaySteps);
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.Claims.ToArray();
            var loginUserEmail = claim[1].Value;
            ObjectId loginUserId = new ObjectId(User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value);
            var stepsToShow = new List<StepsInPathway>();
            ObjectId defaultId = new ObjectId();
            var allpathSteps = _peopleRepository.AsQueryable().Where(steps => steps.PathwayId == id).ToList();
            foreach (var pathSteps in allpathSteps)
            {
                //var checkrequest = _checkRequest.AsQueryable()
                //    .Where(req => req.PathwayStepId.Equals(pathSteps.Id) && req.UserId.Equals(loginUserId/*new ObjectId("5efb2f4b7626e523609280cf")*/))
                //    .ToList();
                var badgeImage = pathSteps.GetBadges != null ? _badge.FindById(pathSteps.GetBadges).ImageUrl.ToString() : null;
                StepsInPathway steps = new StepsInPathway()
                {
                    StepId = null,
                    StepName = pathSteps.StepName,
                    Description = pathSteps.Description,
                    Documents = pathSteps.Documents,
                    Count = pathSteps.count,
                    GetBadges = pathSteps.GetBadges,
                    BadgeImageURL = badgeImage,
                    IsCompleted = null,
                    IsApproved = null,
                    IsDeclined = null

                };
                stepsToShow.Add(steps);

            }
            return View(stepsToShow);
        }

        public IActionResult Create()
        {

           
            //PathwayVM pathwayVM = new PathwayVM()
            //{
            //    GetIssuersinList = _issuer.AsQueryable().ToList(),
                
            //};
            return View();
        }

        [HttpPost]
        public IActionResult Create(Pathways pathway)
        {
            try
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;

                var claim = claimsIdentity.Claims.ToArray();
                var email = claim[1].Value;
                var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;            //claim[3].Value;
                DateTime today = DateTime.Now;
                var user = _users.FindById(userid.ToString());
                var issuers = _issuer.AsQueryable().ToList();
                var issuer = issuers.Where(e => e.UserId == new ObjectId(userid)).FirstOrDefault();
                pathway.CreatedAt = today;
                pathway.IssuersId = issuer.Id;
                pathway.CreatedBy = issuer.Name;
                _path.InsertOne(pathway);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Please try again later.");
                return View();
            }
            return RedirectToAction("Index");
        }

        public IActionResult ManagePathway(string id)
        {
           
            var path = _path.FindById(id);
            var issuerid = path.IssuersId;

            List<PathwaySteps> all = _peopleRepository.AsQueryable().ToList();
            List<PathwaySteps> list = all.Where(e => e.PathwayId == id).ToList();
            if (list == null)
            {
                ModelState.AddModelError(string.Empty,"Pathway id is null");
            }
            PathwayCreation pathwayCreation = new PathwayCreation()
            {
                GetBadgesinList = _badge.FilterBy(e => e.IssuerId == issuerid).ToList(),
                steps = list,
                pathwayName=path.PathwayName
            };
            
            return View(pathwayCreation);
        }
        [HttpPost]
        [ActionName("ManagePathway")]
        public IActionResult ManagePathwayPost(PathwaySteps pathwaySteps, string id)
        {
            var totalRecord = _peopleRepository.FilterBy(e => e.PathwayId == id);
            var numberofRecord = totalRecord.Count();

            var path = _path.FindById(id);
            var issuerid = path.IssuersId;

            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"Documents");
                    var extenstion = Path.GetExtension(files[0].FileName);
                    using (var filesStreams = new FileStream(Path.Combine(uploads, files[0].FileName), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    pathwaySteps.Documents = files[0].FileName;
                    pathwaySteps.PathwayId = id.ToString();
                    TempData["PathwayStepBadge"] = pathwaySteps.GetBadges;
                }
                if (numberofRecord == 0)
                {
                    pathwaySteps.count = 0;
                }
                pathwaySteps.count = numberofRecord + 1;
                pathwaySteps.IssuerId = issuerid;
                _peopleRepository.InsertOne(pathwaySteps);

            }
            return RedirectToAction("ManagePathway");
        }

        public IActionResult EditPathwaySteps(string editid)
        {
            var pathwaystep = _peopleRepository.FindById(editid);
            var pathwayid = pathwaystep.PathwayId;
            var issuerid = pathwaystep.IssuerId;

            List<PathwaySteps> all = _peopleRepository.AsQueryable().ToList();
            List<PathwaySteps> list = all.Where(e => e.PathwayId == pathwayid).ToList();
            if (list == null)
            {
                ModelState.AddModelError(string.Empty, "Pathway id is null");
            }
            PathwayCreation pathwayCreation = new PathwayCreation()
            {
                GetBadgesinList = _badge.FilterBy(e => e.IssuerId == issuerid).ToList(),
                Id= new ObjectId(editid),
                StepName=pathwaystep.StepName,
                Description=pathwaystep.Description,
                Documents=pathwaystep.Documents,
                GetBadges=pathwaystep.GetBadges,
                PathwayId=pathwaystep.PathwayId,
                IssuerId=pathwaystep.IssuerId
            };
            return View(pathwayCreation);
        }
        [HttpPost]
        [ActionName("EditPathwaySteps")]
        public IActionResult EditPathwaySteps(string editid, PathwaySteps steps)
        {
            steps.Id = new ObjectId(editid);
            var editstepid = _peopleRepository.FindById(editid);
            var pathwayid = editstepid.PathwayId;

            if (ModelState.IsValid)
            {
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"Documents");
                    var extenstion = Path.GetExtension(files[0].FileName);
                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName + extenstion), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    steps.Documents = files[0].FileName;
                    steps.PathwayId = editstepid.PathwayId;
                    steps.IssuerId = editstepid.IssuerId;
                    steps.GetBadges = steps.GetBadges;
                    steps.count = editstepid.count;
                }
               
                _peopleRepository.ReplaceOne(steps);
            }
            return RedirectToAction("ManagePathway", new { id = pathwayid });
        }
        public IActionResult DeletePathwaySteps(string delid)
        {
            var deletestepid = _peopleRepository.FindById(delid);
            var pathwayid = deletestepid.PathwayId;
            _peopleRepository.DeleteById(delid);

            var count = 1;
            List<PathwaySteps> pathwaySteps = _peopleRepository.AsQueryable().Where(e => e.PathwayId == pathwayid).ToList();
            foreach (var item in pathwaySteps)
            {
                item.count = count++;
                _peopleRepository.ReplaceOne(item);
            }

            return RedirectToAction("ManagePathway", new { id = pathwayid });
        }

        public IActionResult PathwayEdit(string id)
        {
            var pathway = _path.FindById(id);
            return View(pathway);
        }

        [HttpPost]
        [ActionName("PathwayEdit")]

        public IActionResult PathwayEditPost(string id, Pathways p)
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;//  claim[3].Value;

            var user = _users.FindById(userid.ToString());
            var issuers = _issuer.AsQueryable().ToList();
            var issuer = issuers.Where(e => e.UserId == new ObjectId(userid)).FirstOrDefault();

            p.Id =new ObjectId( id);
            p.IssuersId = issuer.Id;
            p.CreatedBy = issuer.Name;
            _path.ReplaceOne(p);
            return RedirectToAction("Index");
        }

        public IActionResult PathwayDelete(string id)
        {
            var pathwaysteps = _peopleRepository.AsQueryable().ToList();
            var toDeleteSteps = pathwaysteps.Where(e => e.PathwayId == id).ToList();
            if (toDeleteSteps != null)
            {
                _peopleRepository.DeleteMany(e => e.PathwayId == id);
            }
            _path.DeleteById(id);

            return RedirectToAction("Index");
        }

    }
}