using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DigiBadges.DataAccess;
using DigiBadges.DataAccess.Repository;
using DigiBadges.DataAccess.ViewModels;
using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Internal;
using MongoDB.Bson;
using Badge = DigiBadges.DataAccess.Badge;

namespace DigiBadges.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = AppUtility.AdminRole+","+AppUtility.EarnerRole)]
    public class PathwayEarnerController : Controller
    {
        private readonly Repository<Pathways> _path;
        private readonly Repository<PathwaySteps> _pathSteps;
        private readonly Repository<CheckRequest> _checkRequest;
        private readonly Repository<Badge> _badges;
        private readonly Repository<DataAccess.Users> _user;
        private readonly Repository<DataAccess.Issuers> _issuer;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PathwayEarnerController(IWebHostEnvironment hostEnvironment, Repository<DataAccess.Issuers> issuer, Repository<Badge> badge,Repository<Pathways> path, Repository<DataAccess.Users> user, Repository<PathwaySteps> pathSteps, Repository<CheckRequest> checkRequest)
        {
            _path = path;
            _pathSteps = pathSteps;
            _checkRequest = checkRequest;
            _badges = badge;
            _user = user;
            _issuer = issuer;
            _hostEnvironment = hostEnvironment;     

        }
        

        public IActionResult Index(int productPage =1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;

            var pathwaySteps = _pathSteps.AsQueryable().ToList();
            var cr = _checkRequest.AsQueryable().ToList();
            var cr1 = cr.Where(e => e.UserId == new ObjectId(userid.ToString()));

            var issuers = _issuer.AsQueryable().Where(e => e.StaffsIds.Contains(userid)).ToList();
            List<string> allissuerid = new List<string>();
            List<Pathways> validPathways = new List<Pathways>();
            var pathway = _path.AsQueryable().ToList();
            foreach (var item in issuers)
            {
                allissuerid.Add(item.Id.ToString());
            }
            //var issuerid = allissuerid.ToString();

            foreach (var path in pathway)
            {
                if (allissuerid.Contains(path.IssuersId.ToString()))
                {
                    var totalSteps = pathwaySteps.AsQueryable().Where(e=>e.PathwayId == path.Id.ToString()).Count();
                    var completedSteps = cr1.AsQueryable().Where(e => (e.PathwayId == path.Id && e.IsApproved==true)).Count();
                    var completedPercentage = (totalSteps!=0) ? ((completedSteps * 100) / totalSteps):0;
                    path.PathwayCompletion = completedPercentage;
                    validPathways.Add(path);
                }
            }

            PathwayVM pathwayVM = new PathwayVM()
            {
                pathways = validPathways,
                pathwaySteps = pathwaySteps,
                checkRequests = cr1

            };
            var count = pathwayVM.pathways.Count();
            pathwayVM.pathways = pathwayVM.pathways.OrderBy(p => p.PathwayName)
                .Skip((productPage - 1) * 2).Take(2).ToList();

            pathwayVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = 2,
                TotalItems = count,
                urlParam = "/Employee/PathwayEarner/Index?productPage=:"
            };
            return View(pathwayVM);
        }

        public IActionResult Start(string id ,string isCompleted,int currentpage)
        {
            if(isCompleted != null)
            {
                var v = _checkRequest.FindById(isCompleted);
                //CheckRequest c = new CheckRequest() {
                //      IsCompleted = true,
                //     Id= new ObjectId("5efc43e8bbe4823244bd798a")

                //};
                v.IsCompleted = true;
                _checkRequest.ReplaceOne(v);
                return RedirectToAction("Index");
            }
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;

            var all = _pathSteps.AsQueryable().ToList();
           // PathwaySteps p = all.Where(e => e.count == 1 ).Where(e=>e.PathwayId==id).FirstOrDefault();
            PathwaySteps p = all.Where(e => e.count == 1 && e.PathwayId == id).FirstOrDefault();

            if (p != null)
            {
                DateTime today = DateTime.Now;
                CheckRequest check = new CheckRequest()
                {
                    CreatedAt = today,
                    IsApproved = false,
                    IsCompleted = false,
                    PathwayId = new MongoDB.Bson.ObjectId(id),
                    UserId = new ObjectId(userid),
                    PathwayStepId = p.Id



                };
                _checkRequest.InsertOne(check);


            }
                return RedirectToAction("Index",new { productPage = currentpage });
            
          
            
        }

        public IActionResult PathwaySteps(string pathwayId)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.Claims.ToArray();
            var loginUserEmail = claim[1].Value;
            ObjectId loginUserId = new ObjectId(User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value);
            var stepsToShow = new List<StepsInPathway>();
            ObjectId defaultId = new ObjectId();
            var allpathSteps = _pathSteps.AsQueryable().Where(steps=>steps.PathwayId== pathwayId).ToList();
            var pathwayName = _path.FindById(pathwayId).PathwayName;
            foreach (var pathSteps in allpathSteps)
            {   
                var checkrequest = _checkRequest.AsQueryable()
                    .Where(req => req.PathwayStepId.Equals(pathSteps.Id)&&req.UserId.Equals(loginUserId/*new ObjectId("5efb2f4b7626e523609280cf")*/))
                    .ToList();
                var badgeImage = pathSteps.GetBadges != null ? _badges.FindById(pathSteps.GetBadges).ImageUrl.ToString() : null;
                StepsInPathway steps = new StepsInPathway()
                {
                    StepId = checkrequest.Any() ? checkrequest[0].Id : defaultId,
                    PathwayName = pathwayName,
                    StepName = pathSteps.StepName,
                    Description = pathSteps.Description,
                    Documents = pathSteps.Documents,
                    Count = pathSteps.count,
                    GetBadges = pathSteps.GetBadges,
                    BadgeImageURL = badgeImage,
                    IsCompleted = checkrequest.Any() ? checkrequest[0].IsCompleted:null,
                    IsApproved = checkrequest.Any() ? checkrequest[0].IsApproved :null,
                    IsDeclined = checkrequest.Any()? checkrequest[0].IsDeclined:null,
                    DeclineReason= checkrequest.Any() ? checkrequest[0].DeclineReason : null

                };
                stepsToShow.Add(steps);

            }
            return View(stepsToShow);
        }

        

        public IActionResult Step(string id)
        {
            CheckRequest check = _checkRequest.FindById(id);
            PathwaySteps pathway = new PathwaySteps()
            {
                Id = check.PathwayStepId
            };
            List<PathwaySteps> all = _pathSteps.AsQueryable().ToList();
            List<PathwaySteps> list = all.Where(e => e.Id == pathway.Id).ToList();
            PathwayVM pathwayVM = new PathwayVM()
            {
                pathwaySteps = list
            };
            ViewBag.checkRequestId = id;
            return View(pathwayVM);
        }
        [HttpPost]
        [ActionName("Step")]
        public IActionResult StepPost(string id, string isCompleted)
        {
            if (isCompleted != null)
            {
                var v = _checkRequest.FindById(isCompleted);
                //CheckRequest c = new CheckRequest() {
                //      IsCompleted = true,
                //     Id= new ObjectId("5efc43e8bbe4823244bd798a")

                //};
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"UploadedDocuments");
                    var extenstion = Path.GetExtension(files[0].FileName);
                    using (var filesStreams = new FileStream(Path.Combine(uploads, files[0].FileName), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    v.Documents = files[0].FileName;
                }
                v.IsCompleted = true;
                v.IsDeclined = null;
                _checkRequest.ReplaceOne(v);
                return RedirectToAction("PathwaySteps", new { pathwayId = v.PathwayId });
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;

            var all = _pathSteps.AsQueryable().ToList();
            PathwaySteps p = all.Where(e => e.count == 1).Where(e => e.PathwayId == id).FirstOrDefault();

            if (p != null)
            {
                CheckRequest check = new CheckRequest()
                {
                    CreatedAt = DateTime.Now,
                    IsApproved = false,
                    IsCompleted = false,
                    PathwayId = new MongoDB.Bson.ObjectId(id),
                    UserId = new ObjectId(userid),
                    PathwayStepId = p.Id



                };
                _checkRequest.InsertOne(check);


            }
            return RedirectToAction("Index");
           




        }
    }
}