using DigiBadges.Models;
using DigiBadges.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DigiBadges.DataAccess.Data
{
    public class DBInit
    {
        private IConfiguration _config;
        private IMongoCollection<UserRoles> _rolecollection;
        private IMongoCollection<Users> _usersColl;
        private IMongoDatabase _db { get; set; }
        public string AdminRoleId { get; set; }

        public DBInit(IConfiguration config)
        {

            _config = config;

            var client = new MongoClient(config.GetSection("MongoDb").GetSection("ConnectionString").Value);
            _db = client.GetDatabase(config.GetSection("MongoDb").GetSection("Database").Value);


        }

        /// <summary>
        /// Creating super admin
        /// </summary>
        public void CreateSuperAdmin()
        {
            Users user;
            this._usersColl = _db.GetCollection<Users>(typeof(Users).Name);
            var dbset = typeof(Users).Name;
            var collections = _db.ListCollectionNames().ToList();

            if (!collections.Any(x => x == dbset))
            {
                var firstName = _config.GetSection("DBInit").GetSection("SuperUser").GetValue<string>("FirstName");
                var lastName = _config.GetSection("DBInit").GetSection("SuperUser").GetValue<string>("LastName");
                var email = _config.GetSection("DBInit").GetSection("SuperUser").GetValue<string>("Email");
                var passcode = AppUtility.Encrypt(_config.GetSection("DBInit").GetSection("SuperUser").GetValue<string>("Password"));

                user = new Users
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Password = passcode,
                    IsUserVerified = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = AppUtility.DefaultCreatedBy,
                    RoleId = AdminRoleId
                };
                _usersColl.InsertOne(user);
            }
        }

        /// <summary>
        /// Initialize user roles.
        /// </summary>
        public void InitializeUserRoles()
        {
            this._rolecollection = _db.GetCollection<UserRoles>(typeof(UserRoles).Name);
            string[] roles = _config.GetSection("DBInit").GetSection("UserRoles").Value.Split('|');
            var dbset = typeof(UserRoles).Name;
            var collections = _db.ListCollectionNames().ToList();

            if (!collections.Any(x => x == dbset))
            {
                UserRoles ur;
                foreach (string role in roles)
                {
                    ur = new UserRoles();
                    ur.Role = role;
                    _rolecollection.InsertOne(ur);
                }
                AdminRoleId = _rolecollection.Find(e => e.Role == roles[0]).FirstOrDefault().Id.ToString();
            }
        }
    }
}
