using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace DigiBadges.Models
{
  public  class Staff
    {
        [BsonId]
        public ObjectId StaffId { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]

        public string LastName { get; set; }
        [Required]

        public string StaffEmail { get; set; }
        [Required]


        public string StaffRole { get; set; }
    }
}
