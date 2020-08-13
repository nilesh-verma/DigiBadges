using DigiBadges.DataAccess.Repository.IRepository;
using DigiBadges.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigiBadges.DataAccess.Repository
{
    class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }
}
