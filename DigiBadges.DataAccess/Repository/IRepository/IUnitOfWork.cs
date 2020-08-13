using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess.Repository.IRepository
{
    interface IUnitOfWork : IDisposable    {
        void Save();
    }
}
