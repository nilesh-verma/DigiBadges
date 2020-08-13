using Microsoft.AspNetCore.Authentication;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.Models
{
    public class Users
    {
        [BsonId]
        public ObjectId UserId { get; set; }

        [BsonElement]
        [Required]
        [Display(Name = "First Name")]
        public String FirstName { get; set; }
        public String LastName { get; set; }
        
        [BsonElement]
        [Required]
        [EmailAddress]
        public String Email { get; set; }
        
        [BsonElement]
        [Required]
        [Display(Name = "Role")]
        public string RoleId { get; set; }

        [BsonElement]
        [PasswordPropertyText]
        [Required]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public String Password { get; set; }

        [BsonElement]
        public String CreatedBy { get; set; }

        [BsonElement]
        [DisplayFormat(DataFormatString = @"{0:dd\/MM\/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }

        [BsonElement]
        public bool IsUserVerified { get; set; }


    }
}
