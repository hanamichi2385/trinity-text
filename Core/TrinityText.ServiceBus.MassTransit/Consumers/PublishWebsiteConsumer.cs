using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.ServiceBus.Messages;
using TrinityText.ServiceBus.Messages.V1_0;
using V1_0 = TrinityText.ServiceBus.Messages.V1_0;

namespace TrinityText.ServiceBus.MassTransit.Consumers
{
    public class PublishWebsiteConsumer : IConsumer<V1_0.IPublishWebsiteMessage>
    {
        private readonly IPublicationSupportService _generationService;

        private readonly IPublicationService _publicationService;

        private readonly ILogger<PublishWebsiteConsumer> _logger;

        public PublishWebsiteConsumer(IPublicationSupportService generationService, IPublicationService publicationService, ILogger<PublishWebsiteConsumer> logger)
        {
            _generationService = generationService;
            _publicationService = publicationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IPublishWebsiteMessage> context)
        {
            var message = context.Message;
            var mail = new SendMailMessage()
            {
                Id = Guid.NewGuid(),
                IsHtmlBody = true,
            };

            var publicationRs = await _publicationService.Get(message.PublicationId, true);

            if (publicationRs.Success)
            {
                var setting = publicationRs.Value;
                string website = setting.Website;
                string ftpServer = setting.FtpServer.Name;
                string format = setting.DataType.ToString();
                mail.To.Add(setting.Email);
                mail.Subject = string.Format("[CMS] Website {0} updated with success!", website);

                var operationsLogRs = await _generationService.Publish(setting);

                var body = string.Empty;

                if (operationsLogRs.Success)
                {
                    body = "<p>The updating process (<strong>{2}</strong>) for website <strong>{0}</strong> via <strong>{1}</strong> was completed with success!</p>";
                }
                else
                {
                    mail.Subject = string.Format("[CMS] Attention there was an error during updating {0} site", website);

                    body = "<p>There was an error during updating (<strong>{2}</strong>) for website <strong>{0}</strong> via <strong>{1}</strong>!</p>";
                    body += "<p>All processes are terminated, please start a new update if the error is solve</p>";
                    foreach (var e in operationsLogRs.Errors)
                    {
                        body += "<p>" + e.Context + ":" + e.Description + "</p>";
                    }
                }

                mail.Body = string.Format(body, website, ftpServer, format);

                await context.Publish(mail);
                try
                {
                    if (setting.FtpServer != null || (setting.FtpServer == null && setting.ManualDelete == false))
                    {
                        await _publicationService.Remove(setting.Id.Value);
                    }
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
            }
        }
    }
}
