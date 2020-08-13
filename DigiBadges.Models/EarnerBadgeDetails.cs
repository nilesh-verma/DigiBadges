using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.Models
{
    public class EarnerBadgeDetails
    {
        [BsonId]
        public ObjectId EarnerBadgeDetailsId { get; set; }

        public ObjectId UserId { get; set; }

        public ObjectId BadgeId { get; set; }
        public ObjectId PathwayId { get; set; }

        [BsonElement]
        [Display(Name = "Recipient Name")]
        public string RecipientName { get; set; }

        [BsonElement]
        [Required]
        [Display(Name = "Recipient Email")]
        public string RecipientEmail { get; set; }

        [BsonElement]
        [Display(Name = "Awarded Date")]
        
        [DisplayFormat(DataFormatString = @"{0:dd\/MM\/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime AwardedDate { get; set; }

        [BsonElement]
        [Display(Name = "Expiration Date")]
        [DisplayFormat(DataFormatString = @"{0:dd\/MM\/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime ExpirationDate { get; set; }
        [BsonElement]
        [Display(Name = "Is Expired")]
        public Boolean IsExpired { get; set; }
        [BsonElement]
        public string Remark { get; set; }
        [BsonElement]
        [Display(Name = "Document Details")]
        public DocumentDetails DocumentDetails { get; set; }

        
    }
}
