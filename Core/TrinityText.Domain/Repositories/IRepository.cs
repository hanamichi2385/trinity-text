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

        IQueryable<T> Repository { get; }
    }

    public interface IEntity
    {

    }
}
