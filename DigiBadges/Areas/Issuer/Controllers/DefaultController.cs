using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigiBadges.DataAccess;
using DigiBadges.DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DigiBadges.Areas.Issuer.Controllers
{
    [Area("Issuer")]
    public class DefaultController : Controller
    {
        private readonly Repository<Test> _peopleRepository;

    public DefaultController(Repository<Test> peopleRepository)
    {
        _peopleRepository = peopleRepository;
    }
    public async Task<IActionResult> Index()
    {
        var users = new Test()
        {
            FirstName = "Abhishek",
            LastName = "Verma",
            BirthDate = DateTime.Now
        };

        await _peopleRepository.InsertOneAsync(users);               //to post data
        var people = _peopleRepository.FilterBy(                     // to get by passing query
        filter => filter.FirstName != ""
    );
        var user = _peopleRepository.AsQueryable();                 //To get entire data
        var byId = _peopleRepository.FindById("5ef87ab7312dc11e1097e41d");

        var userReplace = new Test()
        {
            Id = new MongoDB.Bson.ObjectId("5ef87ab7312dc11e1097e41d"),
            FirstName = "Saurabh",
            LastName = "Doe",
            BirthDate = DateTime.Now
        };

        _peopleRepository.ReplaceOne(userReplace);                     //Update
                                                                       //var u = _peopleRepository.FindOne(Users.Equals("Saurabh"));

        _peopleRepository.DeleteById("5ef87ab7312dc11e1097e41d");     //delete

        return View();
    }
}
}