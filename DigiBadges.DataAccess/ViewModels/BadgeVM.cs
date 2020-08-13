using DigiBadges.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess.ViewModels
{
    public class BadgeVM
    {
        public IEnumerable<Badge> Badge { get; set; }
    }
}
