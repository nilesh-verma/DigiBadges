using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models.ViewModels
{
    public class CreateUser
    {
        public Users users { get; set; }
        public List<UserRoles> userRoles { get; set; }
    }
}
