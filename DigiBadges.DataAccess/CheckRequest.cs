using DigiBadges.DataAccess.Data;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess
{
    [BsonCollection("CheckRequest")]
    public class CheckRequest:IDocument
    {
        public ObjectId Id { get; set; }
        public DateTime CreatedAt { get; set; }

        public ObjectId PathwayId { get; set; }
        public ObjectId PathwayStepId { get; set; }

        public ObjectId UserId { get; set; }

        public bool? IsCompleted { get; set; }

        public bool? IsApproved { get; set; }
        public string Documents { get; set; }
        public string DeclineReason { get; set; }
        public bool? IsDeclined { get; set; }
    }
}
