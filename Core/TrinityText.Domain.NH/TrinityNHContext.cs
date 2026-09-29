using NHibernate;
using System.Threading.Tasks;
using TrinityText.Domain.Repositories;

namespace TrinityText.Domain.NH
{
    public class TrinityNHContext : ITrinityContext
    {
        //private readonly ISessionFactory _sessionFactory;

        private ISession _session;

        public TrinityNHContext(ISession session)
        {
            _session = session;
            //_sessionFactory = sessionFactory;
        }

        private ITransaction _transaction;

        public ISession CurrentSession
        {
            get
            {
                return _session;
            }
        }

        public string ConnectionString => CurrentSession?.Connection?.ConnectionString;

        // nested Begin/Commit join the outermost transaction (see TrinityEFContext); any Rollback rolls back everything
        private int _transactionDepth;

        public async Task BeginTransaction()
        {
            if (_transaction != null && !_transaction.IsActive)
            {
                // stale transaction (already committed / rolled back)
                _transaction = null;
                _transactionDepth = 0;
            }

            if (_transaction != null)
            {
                _transactionDepth++;
                return;
            }

            _transaction = CurrentSession.BeginTransaction();
            _transactionDepth = 1;
            await Task.CompletedTask;
        }

        public async Task CommitTransaction()
        {
            if (_transaction != null && _transaction.IsActive)
            {
                await CurrentSession.FlushAsync();

                if (_transactionDepth > 1)
                {
                    _transactionDepth--;
                    return;
                }

                await _transaction.CommitAsync();
                _transaction = null;
                _transactionDepth = 0;
            }
        }

        public async Task RollbackTransaction()
        {
            if (_transaction != null && _transaction.IsActive)
            {
                await _transaction.RollbackAsync();
            }

            _transaction = null;
            _transactionDepth = 0;
        }
    }
}
