using DigiBadges.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess.ViewModels
{
    public class PathwayVM
    {
        public Pathways pathway { get; set; }
        public IEnumerable< Pathways> pathways { get; set; }
        public IEnumerable< PathwaySteps> pathwaySteps { get; set; }
        public IEnumerable<CheckRequest> checkRequests { get; set; }
        public PagingInfo PagingInfo { get; set; }
        // public List<Issuers> GetIssuersinList { get; set; }
    }
}
