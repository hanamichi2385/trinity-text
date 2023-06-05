using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityText.Domain.Repositories
{
    public interface ITrinityContext
    {
        Task BeginTransaction();
        Task CommitTransaction();
        Task RollbackTransaction();

        string ConnectionString { get; }
    }
}
