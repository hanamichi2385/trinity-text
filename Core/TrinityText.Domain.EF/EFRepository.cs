using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace TrinityText.Domain.EF
{
    public class EFRepository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly TrinityDbContext _trinityDbContext;

        public EFRepository(TrinityDbContext trinityDbContext)
        {
            _trinityDbContext = trinityDbContext;
        }

        public IQueryable<T> Repository => _trinityDbContext.Set<T>();

        public async Task<T> Create(T newEntity)
        {
            var entity = await _trinityDbContext.AddAsync(newEntity);
            await _trinityDbContext.SaveChangesAsync();

            return entity.Entity;
        }

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
            await _trinityDbContext.Commit();
        }

        public async Task RollbackTransaction()
        {
            await _trinityDbContext.Rollback();
        }
    }
}
