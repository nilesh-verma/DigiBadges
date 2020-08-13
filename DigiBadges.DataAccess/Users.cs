using DigiBadges.DataAccess.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.DataAccess
{
    [BsonCollection("Users")]
    public class Users:IDocument
    {
        
        public ObjectId Id { get; set; }

        [BsonElement]
        [Required]
        public String FirstName { get; set; }
        public String LastName { get; set; }

        [BsonElement]
        [Required]
        [EmailAddress]
        public String Email { get; set; }

        [BsonElement]
        public string RoleId { get; set; }

        [BsonElement]
        [PasswordPropertyText]
        [Required]
        public String Password { get; set; }

        [BsonElement]
        public String CreatedBy { get; set; }

        [BsonElement]
        public DateTime CreatedDate { get; set; }
        [BsonElement]
        public bool IsUserVerified { get; set; }
    }
}
