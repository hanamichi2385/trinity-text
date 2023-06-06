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

        public async Task BeginTransaction()
        {
            if (_transaction == null)
            {
                _transaction = CurrentSession.BeginTransaction();
            }
            else
            {
                if (_transaction.IsActive == false)
                {
                    _transaction.Begin();
                }
            }
            await Task.CompletedTask;
        }

        public async Task CommitTransaction()
        {
            if (_transaction != null && _transaction.IsActive)
            {
                await CurrentSession.FlushAsync();
                await _transaction.CommitAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransaction()
        {
            if (_transaction != null && _transaction.IsActive)
            {
                await _transaction.RollbackAsync();
                _transaction = null;
            }
        }
    }
}
