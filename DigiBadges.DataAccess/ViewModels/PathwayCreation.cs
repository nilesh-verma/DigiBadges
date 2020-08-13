using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess.ViewModels
{
    public class PathwayCreation
    {
        public PathwaySteps pathwaySteps { get; set; }
        public IEnumerable<PathwaySteps> steps { get; set; }
        public List<Badge> GetBadgesinList { get; set; }
        public ObjectId Id { get; set; }
        public DateTime CreatedAt => DateTime.Now;
        public string PathwayId { get; set; }
        public string GetBadges { get; set; }
        public ObjectId IssuerId { get; set; }
        public string StepName { get; set; }
        public string Description { get; set; }
        public string Documents { get; set; }
        public int count { get; set; }
        public string pathwayName { get; set; }
    }
}
