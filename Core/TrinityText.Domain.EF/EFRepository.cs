using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrinityText.Domain.EF
{
    public class EFRepository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly TrinityEFContext _trinityDbContext;

        public EFRepository(TrinityEFContext trinityDbContext)
        {
            _trinityDbContext = trinityDbContext;
        }

        public IQueryable<T> Repository => _trinityDbContext.Set<T>().AsQueryable<T>();

        public string ConnectionString => _trinityDbContext.ConnectionString;

        public async Task<T> Create(T newEntity)
        {
            var entity = await _trinityDbContext.AddAsync(newEntity);
            await _trinityDbContext.SaveChangesAsync();

            return entity.Entity;
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _trinityDbContext.AddRangeAsync(entities);
            await _trinityDbContext.SaveChangesAsync();
        }

        public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> source)
            => source.ToListAsync();

        public Task<TResult> FirstOrDefaultAsync<TResult>(IQueryable<TResult> source)
            => source.FirstOrDefaultAsync();

        public Task<int> CountAsync<TResult>(IQueryable<TResult> source)
            => source.CountAsync();

        public Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> source) where TEntity : class
            => source.ExecuteDeleteAsync();

        public async Task Delete(T entityToDelete)
        {
            _trinityDbContext.Remove(entityToDelete);
            await _trinityDbContext.SaveChangesAsync();
        }

        public async Task<T> Read(params object[] id)
        {
            var entity = await _trinityDbContext.FindAsync<T>(id);

            return entity;
        }

        public async Task<T> Update(T modifiedEntity)
        {
            _trinityDbContext.ChangeTracker.Clear();
            var entity = _trinityDbContext.Update(modifiedEntity);
            await _trinityDbContext.SaveChangesAsync();

            return entity.Entity;
        }

        public async Task BeginTransaction() 
        {
            await _trinityDbContext.BeginTransaction();
        }

        public async Task CommitTransaction()
        {
            await _trinityDbContext.CommitTransaction();
        }

        public async Task RollbackTransaction()
        {
            await _trinityDbContext.RollbackTransaction();
        }
    }
}
