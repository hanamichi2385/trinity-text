using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace TrinityText.Domain.EF
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTrinityWithEntityFramework(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
        {
            //DOMAIN
            services.AddDbContextPool<TrinityEFContext>(optionsAction);
            services.AddScoped(typeof(IRepository<>), typeof(EFRepository<>));

            return services;
        }
    }
}
