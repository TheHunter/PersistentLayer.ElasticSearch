using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class EsTransactionProvider
        : ITransactionProvider
    {
        public bool Exists(string name)
        {
            throw new NotImplementedException();
        }

        public void BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public void BeginTransaction(IsolationLevel? level)
        {
            throw new NotImplementedException();
        }

        public void BeginTransaction(string name)
        {
            throw new NotImplementedException();
        }

        public void BeginTransaction(string name, IsolationLevel? level)
        {
            throw new NotImplementedException();
        }

        public void CommitTransaction()
        {
            throw new NotImplementedException();
        }

        public void RollbackTransaction()
        {
            throw new NotImplementedException();
        }

        public void RollbackTransaction(Exception cause)
        {
            throw new NotImplementedException();
        }

        public bool InProgress { get; private set; }
    }
}
