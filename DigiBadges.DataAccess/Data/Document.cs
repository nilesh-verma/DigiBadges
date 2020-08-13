using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess.Data
{
    public abstract class Document : IDocument
    {
        public ObjectId Id { get; set; }

    
    }
}
