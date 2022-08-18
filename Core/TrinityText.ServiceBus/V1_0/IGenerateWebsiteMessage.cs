using System;

namespace TrinityText.ServiceBus.Messages.V1_0
{
    public interface IGenerateWebsiteMessage
    {
        Guid CorrelationId { get; }
        int FilesGenerationId { get; set; }
        string Host { get; set; }
        Guid Id { get; set; }
    }
}