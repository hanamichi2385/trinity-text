using MassTransit;
using System;
using TrinityText.ServiceBus.MassTransit.Consumers;

namespace TrinityText.ServiceBus.MassTransit.Definitions
{
    public class PublishWebsiteConsumerDefinition : ConsumerDefinition<PublishWebsiteConsumer>
    {
        public PublishWebsiteConsumerDefinition()
        {
            EndpointName = "publishqueue";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<PublishWebsiteConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r => r.Interval(5, TimeSpan.FromMinutes(10)));
            endpointConfigurator.UseInMemoryOutbox();
        }
    }
}
