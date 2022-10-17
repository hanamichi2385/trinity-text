using System;

namespace TrinityText.ServiceBus.Messages.V1_0
{
    public interface IPublishWebsiteMessage
    {
        Guid CorrelationId { get; }
        int PublicationId { get; set; }
        string Host { get; set; }
        Guid Id { get; set; }
    }
}