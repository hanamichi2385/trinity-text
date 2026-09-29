using MassTransit;
using Microsoft.Extensions.Configuration;
using System;
using TrinityText.ServiceBus.MassTransit.Consumers;

namespace TrinityText.ServiceBus.MassTransit.Definitions
{
    public class PublishWebsiteConsumerDefinition : ConsumerDefinition<PublishWebsiteConsumer>
    {
        private readonly int _retry;

        private readonly int _retryIntervalMinutes;
        public PublishWebsiteConsumerDefinition(IConfiguration configuration)
        {
            var options = configuration.GetSection("MassTransit");
            // missing keys used to default to 0: concurrency limit 0 blocks the endpoint, interval 0 retries immediately
            var concurrentLimit = Math.Max(1, options.GetValue<int?>("ConcurrentLimit") ?? 1);
            var retry = Math.Max(0, options.GetValue<int?>("Retry") ?? 0);
            var retryIntervalMinutes = Math.Max(1, options.GetValue<int?>("RetryIntervalMinutes") ?? 1);

            EndpointName = "publish_queue";
            ConcurrentMessageLimit = concurrentLimit;
            _retry = retry;
            _retryIntervalMinutes = retryIntervalMinutes;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<PublishWebsiteConsumer> consumerConfigurator, IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(r => r.Interval(_retry, TimeSpan.FromMinutes(_retryIntervalMinutes)));
            endpointConfigurator.UseInMemoryOutbox(context);
        }
    }
}
