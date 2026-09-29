using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
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

            // without the ZIP: Publish streams it from the database instead of holding it in memory
            var publicationRs = await _publicationService.Get(message.PublicationId, false);
            if (!publicationRs.Success)
            {
                throw new ApplicationException(string.Join(",", publicationRs.Errors.Select(s => s.Description)));
            }

            var setting = publicationRs.Value;

            // already published (redelivered message): nothing to do
            if (setting.StatusCode == PublicationStatus.Success)
            {
                _logger.LogInformation("Publication {id} already completed: publish skipped", setting.Id);
                return;
            }

            // values end up in an HTML mail body: encode them
            string website = WebUtility.HtmlEncode(setting.Website);
            string ftpServer = WebUtility.HtmlEncode(setting.FtpServer?.Name);
            string format = WebUtility.HtmlEncode(setting.DataType.ToString());
            // no line breaks in the mail subject (header injection)
            string subjectWebsite = string.Concat((setting.Website ?? string.Empty).Where(c => !char.IsControl(c)));

            var operationsLogRs = await _generationService.Publish(setting);
            var body = new StringBuilder();
            var subject = new StringBuilder();
            if (operationsLogRs.Success)
            {
                subject.Append($"[CMS] Website {subjectWebsite} update complete with success!");
                body.Append($"<p>The updating process (<strong>{format}</strong>) for website <strong>{website}</strong> via <strong>{ftpServer}</strong> was completed with success!</p>");
            }
            else
            {
                subject.Append($"[CMS] Attention! There was an error during publishing {subjectWebsite} site");

                body.Append($"<p>There was an error during publish process (<strong>{format}</strong>) for website <strong>{website}</strong> via <strong>{ftpServer}</strong>!</p>");
                body.Append("<p>All processes are terminated, please start a new update if the error is solved</p>");
                foreach (var e in operationsLogRs.Errors)
                {
                    body.Append($"<p>{WebUtility.HtmlEncode(e.Context)}:{WebUtility.HtmlEncode(e.Description)}</p>");
                }

                // keep the publication (and its ZIP) so the publish can be started again without regenerating
                var failedRs = await _publicationService.Update(setting.Id.Value, PublicationStatus.Failed, "Website publish failed", null);
                if (!failedRs.Success)
                {
                    _logger.LogError("Unable to mark publication {id} as failed: {errors}", setting.Id, string.Join(",", failedRs.Errors.Select(s => s.Description)));
                }
            }

            if (!string.IsNullOrWhiteSpace(setting.Email))
            {
                await context.Publish(new SendMailMessage()
                {
                    Id = Guid.NewGuid(),
                    IsHtmlBody = true,
                    To = [setting.Email],
                    Subject = subject.ToString(),
                    Body = body.ToString()
                });
            }

            // the row (and the stored ZIP) is removed only after a successful publish
            if (operationsLogRs.Success)
            {
                try
                {
                    if (setting.FtpServer != null || setting.ManualDelete == false)
                    {
                        await _publicationService.Remove(setting.Id.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PUBLISH_WEBSITE_MESSAGE");
                }
            }
        }
    }
}
