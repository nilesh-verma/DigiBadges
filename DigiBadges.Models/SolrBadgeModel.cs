using SolrNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models
{
    public class SolrBadgeModel
    {
        public SolrBadgeModel() { }
        public SolrBadgeModel(string badgename,string badgeId)
        {

            this.BadgeId = badgeId;
            this.BadgeName = badgename;
            
            

        }
        [SolrUniqueKey("BadgeId")]
        public String BadgeId { get; set; }

        //        [SolrUniqueKey("id")]
        [SolrField("id")]
        public String Id { get; set; }

        //[SolrField("CreatedAt")]
        //public DateTime CreatedAt { get; set; }

        [SolrField("Name")]
        public String BadgeName { get; set; }


        

    }
}
