using DigiBadges.DataAccess.Data;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigiBadges.DataAccess
{
    [BsonCollection("PathwaySteps")]
    public class PathwaySteps : IDocument
    {
        public ObjectId Id { get; set; }
        public DateTime CreatedAt => DateTime.Now;
        public string PathwayId { get; set; }
        [Required]
        public string GetBadges { get; set; }
        public ObjectId IssuerId { get; set; }

        [Required]
        public string StepName { get; set; }

        [Required]
        public string Description { get; set; }
        public string Documents { get; set; }
        public int count { get; set; }
    }

}
