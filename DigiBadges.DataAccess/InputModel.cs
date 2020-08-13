using DigiBadges.DataAccess.Data;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess
{
    [BsonCollection("Register")]
    public class InputModel:IDocument
    {
        
        public ObjectId Id { get; set; }

        
   
        public string FirstName { get; set; }

        
    
        public string LastName { get; set; }

                
        
        public string Email { get; set; }

       
        public string Password { get; set; }

       
        public string ConfirmPassword { get; set; }
    }
}
