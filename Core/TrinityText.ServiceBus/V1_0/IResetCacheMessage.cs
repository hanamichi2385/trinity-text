using System;

namespace TrinityText.ServiceBus.Messages.V1_0
{
    public interface IResetCacheMessage
    {
        int EnvironmentType { get; set; }
        string Website { get; set; }

        Guid Id { get; set; }

        Guid CorrelationId { get; set; }
    }
}