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
    public class SolrIssuersModel
    {
        public SolrIssuersModel (){}
        public SolrIssuersModel (Issuers isusr){
            
            this.IssuerId = isusr.IssuerId.ToString();
            this.Image = isusr.Image;
            this.Name = isusr.Name;
            this.WebsiteUrl = isusr.WebsiteUrl;
            this.Email = isusr.Email;
            this.Description = isusr.Description;            
            this.UserId = isusr.UserId.ToString();
            this.Staffsobject = isusr.Staffsobject;
            this.StaffsIds = isusr.StaffsIds;
            this.CreatedDate = isusr.CreatedDate;            

        }
       [SolrUniqueKey("IssuerId")]
        public String IssuerId { get; set; }

//        [SolrUniqueKey("id")]
       [SolrField("id")]
        public String Id { get; set; }
        
        [SolrField("Image")]
        public String Image { get; set; }

        [SolrField("Name")]
        public String Name { get; set; }
        
        [SolrField("WebsiteUrl")]
        public String WebsiteUrl { get; set; }
        
        [SolrField("Email")]
        public string Email { get; set; }

        [SolrField("Description")]
        public String Description { get; set; }

        [SolrField("UserId")]
        public String UserId { get; set; }
        [SolrField("Staffsobject")]
        public Users[] Staffsobject { get; set; }

        [SolrField("StaffsIds")]
        public String[] StaffsIds { get; set; }

        [SolrField("CreatedDate")]
        public DateTime CreatedDate { get; set; }
        
    }
}
