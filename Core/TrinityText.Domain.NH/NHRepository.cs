using NHibernate.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrinityText.Domain.NH
{
    public class NHRepository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly TrinityNHContext _dataContext;

        public string ConnectionString => _dataContext.ConnectionString;

        public IQueryable<T> Repository => _dataContext.CurrentSession.Query<T>();

        public NHRepository(TrinityNHContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task BeginTransaction()
        {
            await _dataContext.BeginTransaction();
        }

        public async Task CommitTransaction()
        {
            await _dataContext.CommitTransaction();
        }

        public async Task RollbackTransaction()
        {
            await _dataContext.RollbackTransaction();
        }

        public async Task<T> Create(T newEntity)
        {
            var entity = await _dataContext.CurrentSession.SaveAsync(newEntity);

            return (T)entity;
        }

        public async Task Delete(T entityToDelete)
        {
            await _dataContext.CurrentSession.DeleteAsync(entityToDelete);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                await _dataContext.CurrentSession.SaveAsync(entity);
            }
            await _dataContext.CurrentSession.FlushAsync();
        }

        public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> source)
            => LinqExtensionMethods.ToListAsync(source);

        public Task<TResult> FirstOrDefaultAsync<TResult>(IQueryable<TResult> source)
            => LinqExtensionMethods.FirstOrDefaultAsync(source);

        public Task<int> CountAsync<TResult>(IQueryable<TResult> source)
            => LinqExtensionMethods.CountAsync(source);

        public Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> source) where TEntity : class
            => DmlExtensionMethods.DeleteAsync(source);

        public async Task<T> Read(params object[] id)
        {
            var entity = await _dataContext.CurrentSession.GetAsync<T>(id);

            return entity;
        }

       

        public async Task<T> Update(T modifiedEntity)
        {
            await _dataContext.CurrentSession.UpdateAsync(modifiedEntity);

            return modifiedEntity;
        }
    }
}