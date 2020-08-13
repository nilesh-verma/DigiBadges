using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigiBadges.Models;
using DigiBadges.Models.ViewModels;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Distributed;
using SolrNet;
using SolrNet.Commands.Parameters;
using SolrNet.Utils;

namespace DigiBadgesNew.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SearchController : Controller
    {
        private readonly IDistributedCache cache;
        private ISolrOperations<SolrUsersModel> _solr;

        private SolrUsersModel _solrUsrModel;
        
        public SearchController(IDistributedCache c,ISolrOperations<SolrUsersModel> solr)
        {
            cache = c;
            _solr = solr;           
        }
        
        
        [HttpGet]
        public IActionResult Index()
        {
            

            SearchVM model = new SearchVM()
            {
                usrList = GetAllQuery()
            };
           
            return View(model);
        }
        public  List<SolrUsersModel> GetAllQuery()
        {
            var results = _solr.Query(SolrQuery.All);

            return results.ToList<SolrUsersModel>();
            //cache.SetString("12345", "aa");
            //List<SolrUsersModel> solrList = new List<SolrUsersModel>();
            //SolrUsersModel test = new SolrUsersModel();
            //test.FirstName = cache.GetString("12345");
            //test.LastName = "B";
            //test.IsUserVerified = true;
            //test.Email = "a@a";
            //solrList.Add(test);
            //return solrList;
        }

    
       public List<SolrUsersModel> GellSelectedQuery(string query)
       {
  
            var results = _solr.Query(query);
   
            return results.ToList<SolrUsersModel>();
       }

        [HttpPost]
        public async Task<IActionResult> Index(SearchVM ssvm)
        {
             if(ssvm.FirstName!=null || ssvm.LastName!= null || ssvm.Email!= null)
            {
                SearchCriteria criteria = new SearchCriteria();

                criteria.FirstName = ssvm.FirstName;
                criteria.LastName = ssvm.LastName;
                criteria.Email = ssvm.Email;


                var searchId = criteria.GetSearchId();

                var usersVal = await cache.GetSearchResultsAsync(searchId);

                    if (usersVal == null)
                    {
                         // string fname = criteria.FirstName;
                        // string lname = criteria.LastName;
                        // string email = criteria.Email;

                         string fname = string.Empty;
                        string lname = string.Empty;
                        string email = string.Empty;

                        if(!string.IsNullOrEmpty(criteria.FirstName))
                        fname = "*" + criteria.FirstName + "*";
                        if(!string.IsNullOrEmpty(criteria.LastName))
                        lname = "*" + criteria.LastName + "*";
                        if(!string.IsNullOrEmpty(criteria.Email))
                        email = "*" + criteria.Email + "*";


                        string query ="FirstName:'" + criteria.FirstName + "' OR " + " LastName:'" + criteria.LastName + "'" + " OR " + " Email:'" + criteria.Email + "'";


                      // var query = Query.Field("FirstName").Is(criteria.FirstName) || Query.Field("LastName").Is(LastName)|| Query.Field("Email").Is(Email);
                     //var results = solr.Query(query);


                        ssvm.usrList = GellSelectedQuery(query);

                        await cache.AddSearchResultsAsync(searchId, ssvm.usrList);

                    }
                    else
                    {
                        ssvm.usrList = new List<SolrUsersModel>();

                         foreach(SolrUsersModel uss in usersVal)
                        {
                            ssvm.usrList.Add(uss);
                        }

                    }

                return View(ssvm);
            }
            else
            {

                 ssvm.usrList = GetAllQuery();
                 return View(ssvm);
            }
            }

            //cache.SetString("12345", "aa");
            //ssvm.FirstName = "Nilesh";
            //ssvm.LastName = "Nilesh";
            //ssvm.Email = "nilesh@gmail.com";
            //return View(ssvm);
        }
        //}
}