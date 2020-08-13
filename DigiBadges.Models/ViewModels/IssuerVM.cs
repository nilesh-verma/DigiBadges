using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models.ViewModels
{
    public class IssuerVM
    {
        public IEnumerable<Issuers> issuers { get; set; }
        public PagingInfo PagingInfo { get; set; }
    }
}
