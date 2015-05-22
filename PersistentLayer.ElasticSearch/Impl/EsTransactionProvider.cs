using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Nest;
using Newtonsoft.Json;
using PersistentLayer.ElasticSearch.Cache;
using PersistentLayer.Exceptions;
using PersistentLayer.Impl;

namespace PersistentLayer.ElasticSearch.Impl
{
    public class EsTransactionProvider
        : ITransactionProvider
    {
        /// <summary>
        /// The default naming
        /// </summary>
        private const string DefaultNaming = "anonymous";
        private readonly Stack<ITransactionInfo> transactions;
        private IElasticClient client;
        private IElasticSession session;

        public EsTransactionProvider(IElasticClient client, JsonSerializerSettings jsonSettings)
        {
            this.client = client;
            this.transactions = new Stack<ITransactionInfo>();
            this.session = new ElasticSession(client.Infer.DefaultIndex, () => this.InProgress, jsonSettings, client);
        }

        public bool Exists(string name)
        {
            if (name == null)
                return false;

            return this.transactions.Count(info => info.Name == name) > 0;
        }

        public void BeginTransaction()
        {
            this.BeginTransaction((IsolationLevel?)null);
        }

        public void BeginTransaction(IsolationLevel? level)
        {
            int index = transactions.Count;
            this.BeginTransaction(string.Format("{0}_{1}", DefaultNaming, index), level);
        }

        public void BeginTransaction(string name)
        {
            this.BeginTransaction(name, (IsolationLevel?)null);
        }

        public void BeginTransaction(string name, IsolationLevel? level)
        {
            if (name == null || name.Trim().Equals(string.Empty))
                throw new BusinessLayerException("The transaction name cannot be null or empty", "BeginTransaction");

            if (this.Exists(name))
                throw new BusinessLayerException(string.Format("The transaction name ({0}) to add is used by another point.", name), "BeginTransaction");

            int index = this.transactions.Count;
            
            if (this.transactions.Count == 0)
            {
                ITransactionInfo info = new TransactionInfo(name, index);
                try
                {
                    //ISession session = this.GetCurrentSession();
                    //if (level == null)
                    //    session.BeginTransaction();
                    //else
                    //    session.BeginTransaction(level.Value);
                    var status = this.client.Ping();
                    if (!status.ConnectionStatus.Success)
                        throw new BusinessLayerException("The service doesn't respond.", "BeginTransaction");
                }
                catch (Exception ex)
                {
                    throw new BusinessLayerException("Error on beginning a new transaction.", "BeginTransaction", ex);
                }

                this.transactions.Push(info);
            }
        }

        public void CommitTransaction()
        {
            if (this.transactions.Count == 1)
            {
                var info = this.transactions.Peek();
                try
                {
                    this.session.Flush();
                    this.transactions.Pop();
                }
                catch (Exception ex)
                {
                    throw new CommitFailedException(string.Format("Error when the current session tries to commit the current transaction (name: {0}).", info.Name), "CommitTransaction", ex);
                }
            }
        }

        public void RollbackTransaction()
        {
            this.RollbackTransaction(null);
        }

        public void RollbackTransaction(Exception cause)
        {
            if (this.transactions.Count > 0)
            {
                this.session.Evict();
                var info = this.transactions.Pop();

                if (this.transactions.Count > 0)
                    throw new InnerRollBackException("An inner rollback transaction has occurred.", cause, info);
            }
        }

        public bool InProgress
        {
            get { return this.transactions.Count > 0; }
        }
    }
}
