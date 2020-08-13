using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models.ViewModels
{
    public class IssuerBadge
    {
        public Issuers issuer { get; set; }
        public IEnumerable<Badge> badge { get; set; }
        public IEnumerable<EarnerBadgeDetails> EarnerBadgeDetails { get; set; }
        public Badge badge1 { get; set; }
        public int Badge_Count { get; set; }
        public bool IsBadgeAvailableInEarner { get; set; }
        public String Id { get; set; }

        public IEnumerable<Users> users { get; set; }
        public PagingInfo PagingInfo { get; set; }

       
    }
}
