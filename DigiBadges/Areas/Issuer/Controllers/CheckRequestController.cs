using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DigiBadges.DataAccess;
using DigiBadges.DataAccess.Repository;
using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using EarnerBadgeDetails = DigiBadges.DataAccess.EarnerBadgeDetails;
using Issuers = DigiBadges.DataAccess.Issuers;
using Users = DigiBadges.DataAccess.Users;

namespace DigiBadges.Areas.Issuer.Controllers
{
    [Area(AppUtility.IssuerRole)]
    [Authorize(Roles = AppUtility.IssuerRole)]
    public class CheckRequestController : Controller
    {
        public Repository<CheckRequest> _checkRequest;
        public Repository<Pathways> _pathway;
        public Repository<PathwaySteps> _pathwaySteps;
        private readonly Repository<Users> _user;
        private readonly IEmailSender _emailSender;
        public Repository<Issuers> _issuer;
        public Repository<EarnerBadgeDetails> _earnerDetails;
        private readonly Repository<DataAccess.Badge> _badge;

        public CheckRequestController(Repository<CheckRequest> checkRequest, Repository<EarnerBadgeDetails> earnerDetails, Repository<DataAccess.Badge> badge, Repository<Issuers> issuer, IEmailSender emailSender, Repository<PathwaySteps> pathwaySteps, Repository<Pathways> pathway, Repository<Users> user)
        {
            _checkRequest = checkRequest;
            _earnerDetails = earnerDetails;
            _pathwaySteps = pathwaySteps;
            _pathway = pathway;
            _user = user;
            _emailSender = emailSender;
            _badge = badge;
            _issuer = issuer;
        }
        public IActionResult Index()
        {
            var completePathway = new List<CompletedPathway>();
            var request = _checkRequest.AsQueryable().ToList();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;

            var issuer = _issuer.AsQueryable().ToList();
            var actualIssuer = issuer.Where(e => e.UserId == new MongoDB.Bson.ObjectId(userid)).FirstOrDefault();
            var pathstep = _pathwaySteps.AsQueryable().ToList();
            var actualstep = pathstep.Where(e => e.IssuerId == actualIssuer.Id).ToList();
            var req = new List<CheckRequest>();
            for (int x = 0; x < actualstep.Count; x++)
            {
                var creq = request.Where(e => e.PathwayStepId == new MongoDB.Bson.ObjectId(actualstep[x].Id.ToString())).ToList();
                for (int creqCountAccordingToSteps = 0; creqCountAccordingToSteps < creq.Count; creqCountAccordingToSteps++)
                {
                    CheckRequest crp = creq[creqCountAccordingToSteps];
                    if (crp != null)
                    {
                        req.Add(crp);
                    }
                }


            }


            for (int a = 0; a < req.Count; a++)
            {
                CheckRequest cr = req[a];
                var p = _pathway.FindById(cr.PathwayId.ToString());
                var uid = _user.FindById(cr.UserId.ToString());





                var pathwaySteps = _pathwaySteps.FindById(cr.PathwayStepId.ToString());



                CompletedPathway c = new CompletedPathway()
                {
                    id = cr.Id,
                    IsApproved = cr.IsApproved,
                    IsCompleted = cr.IsCompleted,
                    Name = uid.FirstName + uid.LastName,
                    PathwayName = p.PathwayName,
                    StepName = pathwaySteps.StepName,
                    uploadedDocuments = cr.Documents,
                    IsDeclined = cr.IsDeclined
                };


                completePathway.Add(c);

            }
            return View(completePathway);
        }
        public async Task<IActionResult> Approve(string id)
        {
            var completePathway = new List<CompletedPathway>();
            var request = _checkRequest.AsQueryable().ToList();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            /*var claim = claimsIdentity.FindFirst(ClaimTypes.Email);*/
            var claim = claimsIdentity.Claims.ToArray();
            var email = claim[1].Value;
            var userid = User.Claims.FirstOrDefault(c => c.Type == AppUtility.UserId).Value;

            var a = _checkRequest.FindById(id);
            a.IsApproved = true;
            a.IsDeclined = false;
            _checkRequest.ReplaceOne(a);



            if (a.IsApproved == true)
            {
                var user = _user.FindById(a.UserId.ToString());
                var pathstep = _pathwaySteps.FindById(a.PathwayStepId.ToString());
                var bdge = _badge.FindById(pathstep.GetBadges);
                var urlOfImage = "http://13.90.135.29/" + bdge.ImageUrl.Replace('\\', '/');
                await _emailSender.SendEmailAsync(user.Email,
                    "Congratulations You are awarded a badge",
                                       $"<div class='p-6 m-3 border rounded row' style='background-color:beige'>" +
                                       $"<div class='col-3'></div><div class='col-6' style='background-color:white'>" +
                                       $"<div class='row text-center m-2'>" +
                                       $"<h3 style = 'color:blueviolet' >" +
                                       $"Congratulation, you earned a badge</h3></div><hr />" +
                                       $"<div class='text-center'>" +
                                       $"<img src = '{urlOfImage}' width='200px' height='200px' alt='image' /></div>" +
                                       $"<hr /><br /><div class='row text-left'>" +
                                       //$"<div class='row text-left'>" +
                                       $"<p class='m-2'>{HtmlEncoder.Default.Encode(bdge.BadgeName)}</p></div>" +

                                       $"<p class='m-2'>{HtmlEncoder.Default.Encode(bdge.EarningCriteriaDescription)}</p></div>" +
                                       /* $"<hr /><hr /><div class='row text-left m-2'>" +
                                        $"<p>Issued by :</p></div><div class='row text-left m-2'><p>" +
                                        $"<b>{HtmlEncoder.Default.Encode(issuer.Name)}</b></p></div><hr />" +
                                        $"<div class='text-center'>" +*/
                                       /*$"<a class='btn btn-secondary' href='https://localhost:44326/'>Create Account</a></div>"*/
                                       $"<br /><br/></div><div class='col-3'></div></div>");

                var pathwayId = a.PathwayId.ToString();
                var totalPathwaySteps = _pathwaySteps.AsQueryable().ToList();
                var totalCheckRequest = _checkRequest.AsQueryable().ToList();
                var countOfPathstepsinCheckRequest = totalCheckRequest.Where(e => e.PathwayId == new MongoDB.Bson.ObjectId(pathwayId)).ToList();
                var count = pathstep.count;
                var p0 = totalPathwaySteps.Where(e => e.PathwayId == pathwayId).ToList();

                PathwaySteps p1 = p0.Where(e => e.count == count).FirstOrDefault();

                PathwaySteps p = totalPathwaySteps.Where(e => e.PathwayId == pathwayId).Where(e => e.count == count + 1).FirstOrDefault();

                if (p1 != null)
                {
                    var badg = _badge.FindById(p1.GetBadges.ToString());
                    DateTime d = DateTime.Now;
                    DateTime d1 = DateTime.Today.AddDays(badg.ExpiryDuration);
                    DateTime today = DateTime.Now;
                    EarnerBadgeDetails earner = new EarnerBadgeDetails()
                    {

                        AwardedDate = today,
                        BadgeId = new MongoDB.Bson.ObjectId(p1.GetBadges),
                        RecipientEmail = user.Email,
                        RecipientName = user.FirstName,
                        ExpirationDate = d1,
                        UserId = a.UserId,
                        PathwayId = new MongoDB.Bson.ObjectId(p1.PathwayId),
                        IsExpired = false


                    };

                    _earnerDetails.InsertOne(earner);



                }
                if (p != null)
                {

                    DateTime today = DateTime.Now;


                    CheckRequest check = new CheckRequest()
                    {
                        CreatedAt = today,
                        IsApproved = false,
                        IsCompleted = false,
                        PathwayId = new MongoDB.Bson.ObjectId(a.PathwayId.ToString()),
                        UserId = a.UserId,
                        PathwayStepId = p.Id



                    };
                    _checkRequest.InsertOne(check);
                }
            }
            return RedirectToAction("Index");
        }
        public IActionResult Decline(string id, string reason)
        {
            CheckRequest check = _checkRequest.FindById(id);
            if (check != null)
            {
                string msg = string.Empty;
                if (!string.IsNullOrEmpty(reason))
                    msg = reason.Trim().TrimEnd('.') + '.';

                check.IsDeclined = true;
                check.DeclineReason = msg;
                _checkRequest.ReplaceOne(check);
            }
            return RedirectToAction("Index");
        }
    }
}