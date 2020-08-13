using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models
{
    public class ShareTableLog
    {
        [BsonId]
        public ObjectId ShareLogId { get; set; }

        [BsonElement]
        public ObjectId UserId { get; set; }

        [BsonElement]
        public ObjectId CollectionId { get; set; }

        [BsonElement]
        public string[] BadgeId { get; set; }

        [BsonElement]
        public string[] PlatformNameArrays { get; set; }
    }
}
