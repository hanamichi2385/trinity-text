using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrinityText.Business;
using TrinityText.ServiceBus.MassTransit.Consumers;
using TrinityText.ServiceBus.MassTransit.Definitions;
using TrinityText.ServiceBus.MassTransit.Services;

namespace TrinityText.ServiceBus.MassTransit
{
    public static class ServiceCollectionsExtensions
    {
        public static IServiceCollection WithTrinityMassTransitConsumers(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PublicationSupportOptions>(opt => configuration.GetSection("PublicationSupportOptions").Bind(opt));
            services.AddScoped<IPublicationSupportService, MassTransitPublicationSupportService>();
            services.AddScoped<PublishWebsiteConsumer>();
            services.AddScoped<GenerateWebsiteConsumer>();

            return services;
        }

        public static IBusRegistrationConfigurator AddTrinityConsumers(this IBusRegistrationConfigurator bsc)
        {
            bsc.AddConsumer<GenerateWebsiteConsumer>(typeof(GenerateWebsiteConsumerDefinition));
            bsc.AddConsumer<PublishWebsiteConsumer>(typeof(PublishWebsiteConsumerDefinition));
            return bsc;
        }
    }
}
