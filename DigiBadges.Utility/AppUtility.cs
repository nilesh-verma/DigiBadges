using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.Utility
{
    public static class AppUtility
    {

        public static string Encrypt(string password)
        {
            string msg = string.Empty;
            byte[] encode = new byte[password.Length];
            encode = Encoding.UTF8.GetBytes(password);
            msg = Convert.ToBase64String(encode);
            return msg;
        }


        #region User

        public const string AdminRole = "Admin";
        public const string EarnerRole = "Earner";
        public const string IssuerRole = "Issuer";
        public const string IssuerPassword = "Welcome@123";

        public static string DefaultCreatedBy = "SelfCreatedUser";
        public static string UserId = "UserId";
        public static string LoginHistoryId = "LoginHistoryId";

        #endregion     
    }
}
