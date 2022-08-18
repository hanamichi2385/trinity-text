using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrinityText.ServiceBus.Messages.V1_0;

namespace TrinityText.ServiceBus.Messages
{
    public class SendMailMessage : ISendMailMessage
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

        public SendMailMessage()
        {
            To = new List<string>();
            AttachmentFiles = new List<SerializedFile>();
        }

        public string From { get; set; }

        public List<string> To { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public bool IsHtmlBody { get; set; }

        public IList<SerializedFile> AttachmentFiles { get; set; }
    }

    public class SerializedFile
    {
        public SerializedFile()
        {
        }

        public SerializedFile(string fullFilenamePath)
        {
            string name = fullFilenamePath.Split('\\').Last();
            byte[] bytes = File.ReadAllBytes(fullFilenamePath);

            this.FileName = name;
            this.FileContent = bytes;
        }

        public SerializedFile(string filename, byte[] content)
        {
            this.FileName = filename;
            this.FileContent = content;
        }

        public string FileName { get; set; }

        public byte[] FileContent { get; set; }
    }
}
