using MassTransit;
using System;
using TrinityText.ServiceBus.MassTransit.Consumers;

namespace TrinityText.ServiceBus.MassTransit.Definitions
{
    public class GenerateWebsiteConsumerDefinition : ConsumerDefinition<GenerateWebsiteConsumer>
    {
        public GenerateWebsiteConsumerDefinition()
        {
            EndpointName = "generatequeue";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<GenerateWebsiteConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r => r.Interval(5, TimeSpan.FromMinutes(10)));
            endpointConfigurator.UseInMemoryOutbox();
        }
    }
}
