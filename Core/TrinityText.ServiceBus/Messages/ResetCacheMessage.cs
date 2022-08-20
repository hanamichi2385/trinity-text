using System;
using TrinityText.ServiceBus.Messages.V1_0;

namespace TrinityText.ServiceBus.Messages
{
    public class ResetCacheMessage : IResetCacheMessage
    {
        public string Website { get; set; }

        public int EnvironmentType { get; set; }
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
    }
}
