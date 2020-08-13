using DigiBadges.DataAccess.Data;
using DigiBadges.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.DataAccess
{
    [BsonCollection("Badges")]
    public class Badge : IDocument
    {
        public ObjectId Id { get; set; }
        public DateTime CreatedAt => DateTime.Now;

        [Display(Name = "Name")]
        [Required]
        public string BadgeName { get; set; }

        [Display(Name = "Path")]
        public string ImageUrl { get; set; }
     
        public double ExpiryDuration { get; set; }

        public string EarningCriteriaDescription { get; set; }

        //public string[] Tags { get; set; }

        //public Alignment Alignment { get; set; }
        [BsonElement]
        [DisplayFormat(DataFormatString = @"{0:dd\/MM\/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }

        public ObjectId IssuerId { get; set; }

        [Required(ErrorMessage = "Please click on facebook button to get sharing URL")]
        public string FacebookId { get; set; }
        public string CreatedBy { get; set; }
    }
}
