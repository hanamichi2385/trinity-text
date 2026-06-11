using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
            To = [];
            AttachmentFiles = [];
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

        public SerializedFile(string filename, byte[] content)
        {
            this.FileName = filename;
            this.FileContent = content;
        }

        public static async Task<SerializedFile> FromFileAsync(string fullFilenamePath)
        {
            var name = Path.GetFileName(fullFilenamePath);
            var bytes = await File.ReadAllBytesAsync(fullFilenamePath);
            return new SerializedFile(name, bytes);
        }

        public string FileName { get; set; }

        public byte[] FileContent { get; set; }
    }
}
