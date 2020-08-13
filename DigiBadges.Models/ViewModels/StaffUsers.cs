using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models.ViewModels
{
  public  class StaffUsers
    {
        public Users Users { get; set; }

        public List<UserRoles> UserRoles { get; set; }
    }
}
