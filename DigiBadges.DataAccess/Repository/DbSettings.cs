using DigiBadges.DataAccess.IRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess.Repository
{
    class DbSettings : IDbSettings
    {
        public string BooksCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}
