using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.Models
{
    public class Issuers
    {
        [BsonId]
        public ObjectId IssuerId { get; set; }
        [BsonElement]

        
        public string Image { get; set; }

        [BsonElement]
        [Required]
        public string Name { get; set; }


        [BsonElement]
         [Required]
         [Url]
         [Display(Name= "Url")]
        public string WebsiteUrl { get; set; }

        [BsonElement]
        [Required]
        [EmailAddress]
        public string Email { get; set; }


        [Required]

        [BsonElement]
        public string Description { get; set; }

        [BsonElement]
        public ObjectId UserId { get; set; }
        public Users[] Staffsobject { get; set; }
        public string[] StaffsIds { get; set; }

        [Display(Name = "Created Date")]
        [BsonElement]
        [DisplayFormat(DataFormatString = @"{0:dd\/MM\/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }

    }
}
