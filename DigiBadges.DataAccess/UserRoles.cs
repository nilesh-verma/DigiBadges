using DigiBadges.DataAccess.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.DataAccess
{
    [BsonCollection("UserRoles")]
    public class UserRoles :IDocument
    {
       
        public ObjectId Id { get; set; }

        [BsonElement]
        [Required]
        public string Role { get; set; }
    }
}
