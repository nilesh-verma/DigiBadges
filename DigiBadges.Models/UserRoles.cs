using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.Models
{
    public class UserRoles
    {
        [BsonId]
        public ObjectId RoleId { get; set; }

        [BsonElement]
        [Required]
        public string Role { get; set; }
    }
}
