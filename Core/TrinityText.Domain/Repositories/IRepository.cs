using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrinityText.Domain
{
    public interface IRepository<T> where T : class, IEntity
    {
        Task<T> Create(T newEntity);
        Task<T> Read(params object[] id);
        Task<T> Update(T modifiedEntity);
        Task Delete(T entityToDelete);
        Task AddRangeAsync(IEnumerable<T> entities);

        Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> source);
        Task<TResult> FirstOrDefaultAsync<TResult>(IQueryable<TResult> source);
        Task<int> CountAsync<TResult>(IQueryable<TResult> source);
        Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> source) where TEntity : class;

        string ConnectionString { get; }

        IQueryable<T> Repository { get; }

        Task BeginTransaction();
        Task CommitTransaction();
        Task RollbackTransaction();
    }

    public interface IEntity
    {

    }
}
