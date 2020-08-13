using DigiBadges.DataAccess.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.DataAccess
{
    [BsonCollection("Issuers")]
    public class Issuers :IDocument
    {
        
        public ObjectId Id { get; set; }
        [BsonElement]

        public string Image { get; set; }

        [BsonElement]
        [Required]
        public string Name { get; set; }


        [BsonElement]
        // [Required]
        // [Url]
        public string WebsiteUrl { get; set; }

        [BsonElement]
        [Required]
        [EmailAddress]
        public string Email { get; set; }


        [Required]

        [BsonElement]
        public string Description { get; set; }

        public ObjectId UserId { get; set; }

        public string[] StaffsIds { get; set; }
        public Users[] Staffsobject { get; set; }

        [BsonElement]
        [DisplayFormat(DataFormatString = @"{0:dd\/MM\/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }

    }
}
