using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models
{
    public class Paths
    {
        public ObjectId StepId { get; set; }    // Checkrequest Collection
        public ObjectId UserId { get; set; }    // Login User
        public string StepName { get; set; }    // PathwaySteps
        public string Name { get; set; }    // Pathway
        public string Description { get; set; } // PathwaySteps
        public string Documents { get; set; }   // PathwaySteps
        public int Count { get; set; }          // PathwaySteps
        public string GetBadges { get; set; }   // PathwaySteps
        public bool IsCompleted { get; set; }   // Checkrequest Collection
        public bool IsApproved { get; set; }    // Checkrequest Collection
    }
}
