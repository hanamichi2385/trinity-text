using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
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

            var publicationRs = await _publicationService.Get(message.PublicationId, true);
            if (publicationRs.Success)
            {
                var setting = publicationRs.Value;
                string website = setting.Website;
                string ftpServer = setting.FtpServer.Name;
                string format = setting.DataType.ToString();

                var operationsLogRs = await _generationService.Publish(setting);
                var body = new StringBuilder();
                var subject = new StringBuilder();
                if (operationsLogRs.Success)
                {
                    subject.Append($"[CMS] Website {website} updated with success!");
                    body.Append($"<p>The updating process (<strong>{format}</strong>) for website <strong>{website}</strong> via <strong>{ftpServer}</strong> was completed with success!</p>");
                }
                else
                {
                    subject.Append($"[CMS] Attention! There was an error during publishing {website} site");

                    body.Append($"<p>There was an error during publish process (<strong>{format}</strong>) for website <strong>{website}</strong> via <strong>{ftpServer}</strong>!</p>");
                    body.Append("<p>All processes are terminated, please start a new update if the error is solved</p>");
                    foreach (var e in operationsLogRs.Errors)
                    {
                        body.Append($"<p>{e.Context}:{e.Description}</p>");
                    }
                }
                var mail = new SendMailMessage()
                {
                    Id = Guid.NewGuid(),
                    IsHtmlBody = true,
                    To = [setting.Email],
                    Subject = subject.ToString(),
                    Body = body.ToString()
                };
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
                    _logger.LogError(ex, $"PUBLISH_WEBSITE_MESSAGE");
                }
            }
            else
            {
                throw new ApplicationException(string.Join(",", publicationRs.Errors.Select(s => s.Description)));
            }
        }
    }
}
