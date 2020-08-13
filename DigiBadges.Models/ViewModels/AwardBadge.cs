using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models.ViewModels
{
    public class AwardBadge
    {
        public string id { get; set; }
        public EarnerBadgeDetails EarnerBadgeDetails { get; set; }
    }
}
