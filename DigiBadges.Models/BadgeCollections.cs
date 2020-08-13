using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.Models
{
  public  class BadgeCollections
    {
        [BsonId]
        [Required]
        public ObjectId CollectionId { get; set; }
        [Required]

        public string CollectionName { get; set; }
        [Required]

        public string CollectionDescription { get; set; }


        public ObjectId UserId { get; set; }

        public string[] BadgeId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
