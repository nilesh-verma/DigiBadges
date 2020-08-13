using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models
{
    public class StepsInPathway
    {
        public string PathwayName { get; set; }
        public ObjectId? StepId       {get;set;} 
        public string StepName     {get;set;} 
        public string Description  {get;set;}     
        public string Documents    {get;set;} 
        public int Count {get;set;}
        public string GetBadges {get;set;}
        public string BadgeImageURL { get; set; }
        public bool? IsCompleted{get;set;} 
        public bool? IsApproved {get;set;}
        public bool? IsDeclined {get;set; }
        public string DeclineReason { get; set; }

    }
}
