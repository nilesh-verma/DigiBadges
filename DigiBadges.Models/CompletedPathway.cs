using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models
{
    public class CompletedPathway
    {
        public ObjectId? id { get; set; }
        public string PathwayName { get; set; }
        public string StepName { get; set; }
        public string Name { get; set; }
        public bool? IsCompleted { get; set; }
        public bool? IsApproved { get; set; }
        public string uploadedDocuments { get; set; }
        public bool? IsDeclined { get; set; }
    }
}
