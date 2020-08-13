
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;

using System.Text;

namespace DigiBadges.Models
{
    public class SearchCriteria
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }       
        public string Email { get; set; }
        public SearchCriteria()
        {
        }

        public SearchCriteria(string firstName, string lastName, string email) : this()
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
        }

        public string GetSearchId()
        {
            return $"Search/{FirstName}/{LastName}/{Email}";
        }


    }
    }