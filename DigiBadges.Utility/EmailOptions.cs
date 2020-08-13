using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Utility
{
    public class EmailOptions
    {
        public string FromAddress { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public bool EnableSSL { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IsBodyHtml { get; set; }
    }
}
