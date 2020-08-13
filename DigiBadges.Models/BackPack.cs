using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models
{
  public class BackPack
    {
        public string BadgeName { get; set; }
        public string earningDescription { get; set; }
        public string IssuerName { get; set; }
        public string ImageUrl { get; set; }

        public string badgeid { get; set; }
        public string FacebookId { get; set; }
        public bool IsExpired { get; set; }
    }
}
