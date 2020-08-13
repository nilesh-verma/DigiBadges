using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DigiBadgesNew.Areas.Identity.Pages.Account
{
    public class verifycodeModel : PageModel
    {
        [BindProperty]
        public VerifyModel verify { get; set; }

        public class VerifyModel
        {
            public string First { get; set; }
            public string Second { get; set; }
            public string Third { get; set; }
            public string Fourth { get; set; }
            public string Fifth { get; set; }
            public string Sixth { get; set; }
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost(string id)
        {
            //var email = TempData["email"];
            var code = id;

            var number = verify.First + verify.Second + verify.Third + verify.Fourth + verify.Fifth + verify.Sixth;
            if (code == number)
            {
                return RedirectToPage("Login");
            }
            else
            {
                return RedirectToPage("Register");
            }
        }
    }
}
