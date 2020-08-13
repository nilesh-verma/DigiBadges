using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.Models
{
    public class Badge
    {
        [BsonId]
        public ObjectId BadgeId { get; set; }

        [BsonElement]
        [Display(Name = "Badge Name")]
        [Required]
        public string BadgeName { get; set; }

        [BsonElement]
        [Display(Name = "Image Path")]
        public string ImageUrl { get; set; }
        [Display(Name = "Expiry Duration")]
       
        public double ExpiryDuration { get; set; }

        [BsonElement]
        [Display(Name = "Earning Criteria Description")]
        [Required]
        public string EarningCriteriaDescription { get; set; }

        //public string[] Tags { get; set; }

        //public Alignment Alignment { get; set; }


        [BsonElement]
        [DisplayFormat(DataFormatString = @"{0:dd\/MM\/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }

        public ObjectId IssuerId { get; set; }
        [Display(Name = "Facebook share url")]
        [Required(ErrorMessage ="Please click on facebook button to get sharing URL")]
        public string FacebookId { get; set; }


        public string CreatedBy { get; set; }


    }
}