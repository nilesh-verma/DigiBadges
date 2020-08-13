using Microsoft.AspNetCore.Authentication;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SolrNet.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.Models
{
    public class SolrUsersModel
    {
        public SolrUsersModel (){}
        public SolrUsersModel (Users usr){
            
            this.UserId = usr.UserId.ToString();
            //this.UserId = usr.UserId;
            this.FirstName = usr.FirstName;
            this.LastName = usr.LastName;
            this.Email = usr.Email;
            this.RoleId = usr.RoleId;
            this.Password = usr.Password;
            this.CreatedBy = usr.CreatedBy;
            this.CreatedDate = usr.CreatedDate;
            this.IsUserVerified = usr.IsUserVerified;

        }
       [SolrUniqueKey("UserId")]
        public String UserId { get; set; }

//        [SolrUniqueKey("id")]
       [SolrField("id")]
        public String Id { get; set; }
        
        [SolrField("FirstName")]
        public String FirstName { get; set; }

        [SolrField("LastName")]
        public String LastName { get; set; }
        
        [SolrField("Email")]
        public String Email { get; set; }
        
        [SolrField("RoleId")]
        public string RoleId { get; set; }

        [SolrField("Password")]
        public String Password { get; set; }

        [SolrField("CreatedBy")]
        public String CreatedBy { get; set; }

        [SolrField("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [SolrField("IsUserVerified")]
        public bool IsUserVerified { get; set; }
        
    }
}
