using System;
using System.Collections.Generic;

namespace TrinityText.ServiceBus.Messages.V1_0
{
    public interface ISendMailMessage
    {
        IList<SerializedFile> AttachmentFiles { get; set; }
        string Body { get; set; }
        Guid CorrelationId { get; }
        string From { get; set; }
        Guid Id { get; set; }
        bool IsHtmlBody { get; set; }
        string Subject { get; set; }
        List<string> To { get; set; }
    }
}