using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;

using System.Text;

namespace DigiBadges.Models
{
    public class AppUser 
    {
        public string FirstName { get; set; }
        public IEnumerable<Claim> Claims { get; set; }

        public string Email { get; set; }
    }
}
