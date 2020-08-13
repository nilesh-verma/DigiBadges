using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Models.ViewModels
{
    public class SearchVM 
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }       
        public string Email { get; set; }       
        public List<SolrUsersModel> usrList { get; set; }
        
    }
}
