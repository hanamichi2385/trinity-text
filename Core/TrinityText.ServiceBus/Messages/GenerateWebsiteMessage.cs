﻿using System;
using TrinityText.ServiceBus.Messages.V1_0;

namespace TrinityText.ServiceBus.Messages
{
    public class GenerateWebsiteMessage : IGenerateWebsiteMessage
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

        public int PublicationId { get; set; }

        public string Host { get; set; }

        #endregion

        public GenerateWebsiteMessage()
        {

        }

        public GenerateWebsiteMessage(int filesGenerationId, string host)
        {
            PublicationId = filesGenerationId;
            Host = host;
        }
    }
}
