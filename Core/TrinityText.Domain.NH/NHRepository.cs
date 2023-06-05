using NHibernate;
using NHibernate.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TrinityText.Domain.NH
{
    public class NHRepository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly ISessionFactory _sessionFactory;

        public string ConnectionString => CurrentSession.Connection.ConnectionString;

        public IQueryable<T> Repository => CurrentSession.Query<T>();

        public NHRepository(ISessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        private ISession CurrentSession
        {
            get
            {
                return _sessionFactory.OpenSession();
            }
        }

        public async Task BeginTransaction()
        {
            CurrentSession.GetCurrentTransaction().Begin();
            await Task.CompletedTask;
        }

        public async Task CommitTransaction()
        {
            await CurrentSession.GetCurrentTransaction().CommitAsync();
        }

        public async Task<T> Create(T newEntity)
        {
            var entity = await CurrentSession.SaveAsync(newEntity);

            return (T)entity;
        }

        public async Task Delete(T entityToDelete)
        {
            await CurrentSession.DeleteAsync(entityToDelete);
        }

        public async Task<T> Read(params object[] id)
        {
            var entity = await CurrentSession.GetAsync<T>(id);

            return entity;
        }

        public async Task RollbackTransaction()
        {
            await CurrentSession.GetCurrentTransaction().RollbackAsync();
        }

        public async Task<T> Update(T modifiedEntity)
        {
            await CurrentSession.UpdateAsync(modifiedEntity);

            return modifiedEntity;
        }
    }
}