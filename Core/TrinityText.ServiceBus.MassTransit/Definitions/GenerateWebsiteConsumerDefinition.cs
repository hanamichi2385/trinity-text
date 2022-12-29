using MassTransit;
using Microsoft.Extensions.Configuration;
using System;
using TrinityText.ServiceBus.MassTransit.Consumers;

namespace TrinityText.ServiceBus.MassTransit.Definitions
{
    public class GenerateWebsiteConsumerDefinition : ConsumerDefinition<GenerateWebsiteConsumer>
    {
        private readonly int _retry;

        private readonly int _retryIntervalMinutes;
        public GenerateWebsiteConsumerDefinition(IConfiguration configuration)
        {
            var options = configuration.GetSection("MassTransit");
            var concurrentLimit = options.GetValue<int>("ConcurrentLimit");
            var retry = options.GetValue<int>("Retry");
            var retryIntervalMinutes = options.GetValue<int>("RetryIntervalMinutes");

            EndpointName = "generate_queue";
            ConcurrentMessageLimit = concurrentLimit;
            _retry = retry;
            _retryIntervalMinutes = retryIntervalMinutes;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<GenerateWebsiteConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r => r.Interval(_retry, TimeSpan.FromMinutes(_retryIntervalMinutes)));
            endpointConfigurator.UseInMemoryOutbox();
        }
    }
}
