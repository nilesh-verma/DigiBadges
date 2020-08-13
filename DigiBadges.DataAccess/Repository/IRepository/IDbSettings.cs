using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess.IRepository
{
    interface IDbSettings
    {
        string BooksCollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}
