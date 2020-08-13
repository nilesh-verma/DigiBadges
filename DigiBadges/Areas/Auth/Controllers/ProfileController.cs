using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigiBadges.Models;

namespace DigiBadges.Areas.Auth.Controllers
{
    public class ProfileController : Controller
    {      
        [Authorize]
        public IActionResult Index()
        {
            var vm = new AppUser
            {
                Claims = User.Claims,
                Email = User.Identity.Name
            };
            return View(vm);
        }
    }
}