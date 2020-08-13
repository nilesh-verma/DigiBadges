using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models
{
    public class LoginHistory
    {
        [BsonId]
        public ObjectId UserILoginID { get; set; }

        [BsonElement]
        public DateTime LoginTime { get; set; }

        [BsonElement]
        public DateTime LogoutTime { get; set; }

        [BsonElement]
        public ObjectId UserId { get; set; }
    }
}
