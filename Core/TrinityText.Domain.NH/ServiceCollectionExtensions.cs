using Microsoft.Extensions.DependencyInjection;
using NHibernate.Cfg;
using NHibernate.NetCore;

namespace TrinityText.Domain.NH
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTrinityWithNHibernate(this IServiceCollection services, Configuration cfg)
        {
            //DOMAIN
            var modelMapper = new TrinityModelMapper();
            cfg.AddMapping(modelMapper.CompileMappingForAllExplicitlyAddedEntities());
            //cfg.BuildSessionFactory();

            services.AddHibernate(cfg);
            services.AddScoped(typeof(TrinityNHContext));
            services.AddScoped(typeof(IRepository<>), typeof(NHRepository<>));

            return services;
        }
    }
}
