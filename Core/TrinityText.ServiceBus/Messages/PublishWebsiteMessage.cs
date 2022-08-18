using System;
using TrinityText.ServiceBus.Messages.V1_0;

namespace TrinityText.ServiceBus.Messages
{
    public class PublishWebsiteMessage : IPublishWebsiteMessage
    {
        public Guid Id
        {
            get;
            set;
        }

        public Guid CorrelationId
        {
            get { return Id; }
        }
        #region Properties

        public int FilesGenerationId { get; set; }

        public string Host { get; set; }

        #endregion

        public PublishWebsiteMessage()
        {
        }

        public PublishWebsiteMessage(int filesGenerationId, string host)
        {
            FilesGenerationId = filesGenerationId;
            Host = host;
        }
    }
}
