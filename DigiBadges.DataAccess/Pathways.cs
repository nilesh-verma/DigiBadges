using DigiBadges.DataAccess.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DigiBadges.DataAccess
{
    [BsonCollection("Pathways")]
    public class Pathways : IDocument
    {
        public ObjectId Id { get ; set; }
        [DisplayFormat(DataFormatString = @"{0:dd\/MM\/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Display(Name = "Pathway Name")]
        public string PathwayName { get; set; }
        [Required]
        [Display(Name = "Institute Name")]
        public string InstituteName { get; set; }
        [Required]
        [Url]
        [Display(Name = "Institute Url")]
        public string InstituteUrl { get; set; }
        [Display(Name = "CreatedBy")]
        public string CreatedBy { get; set; }

        [Required]
        public ObjectId IssuersId { get; set; }
        
        [BsonIgnore]
        public double PathwayCompletion { get; set; }




    }
}
