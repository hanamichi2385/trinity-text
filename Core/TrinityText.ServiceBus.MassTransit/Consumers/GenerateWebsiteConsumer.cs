using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.ServiceBus.Messages;
using V1_0 = TrinityText.ServiceBus.Messages.V1_0;

namespace TrinityText.ServiceBus.MassTransit.Consumers
{
    public class GenerateWebsiteConsumer : IConsumer<V1_0.IGenerateWebsiteMessage>
    {
        private readonly IPublicationSupportService _generationService;

        private readonly IPublicationService _publicationService;

        private readonly ILogger<GenerateWebsiteConsumer> _logger;

        public GenerateWebsiteConsumer(IPublicationSupportService generationService, IPublicationService publicationService, ILogger<GenerateWebsiteConsumer> logger)
        {
            _generationService = generationService;
            _publicationService = publicationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<V1_0.IGenerateWebsiteMessage> context)
        {
            var message = context.Message;

            var publicationSettingRs = await _publicationService.Get(message.PublicationId, false);
            if (!publicationSettingRs.Success)
            {
                throw new Exception(string.Join(",", publicationSettingRs.Errors.Select(s => s.Description)));
            }

            var setting = publicationSettingRs.Value;

            // Redelivery guards (retries / duplicated messages): a completed publication is never regenerated,
            // and one whose ZIP is already stored only needs its publish message sent again.
            if (setting.StatusCode == PublicationStatus.Success)
            {
                _logger.LogInformation("Publication {id} already completed: generation skipped", setting.Id);
                return;
            }

            if (setting.StatusCode == PublicationStatus.Publishing && setting.FtpServer != null)
            {
                _logger.LogInformation("Publication {id} already generated: publish message sent again", setting.Id);
                await context.Publish(new PublishWebsiteMessage(setting.Id.Value, message.Host));
                return;
            }

            var startRs = await _publicationService.Update(setting.Id.Value, PublicationStatus.Generating, "Generation started", null);
            if (!startRs.Success)
            {
                throw new Exception(string.Join(",", startRs.Errors.Select(s => s.Description)));
            }

            var generateRs = await _generationService.Generate(setting);
            string dataType = setting.DataType.ToString();
            string website = setting.Website;
            // no line breaks in the mail subject (header injection)
            string subjectWebsite = string.Concat((setting.Website ?? string.Empty).Where(c => !char.IsControl(c)));

            //operation log empty = generate with success
            if (generateRs.Success)
            {
                bool includePublishing = setting.FtpServer != null;
                if (includePublishing)
                {
                    var rs = await _publicationService.Update(setting.Id.Value, PublicationStatus.Publishing, "Website update is on the way", null);

                    if (rs.Success)
                    {
                        var publishmsg = new PublishWebsiteMessage(setting.Id.Value, message.Host);
                        await context.Publish(publishmsg);
                    }
                    else
                    {
                        throw new Exception(string.Join(",", rs.Errors.Select(s => s.Description)));
                    }
                }
                else
                {
                    var rs = await _publicationService.Update(setting.Id.Value, PublicationStatus.Success, "File created", null);
                    if (!rs.Success)
                    {
                        throw new Exception(string.Join(",", rs.Errors.Select(s => s.Description)));
                    }

                    if (!string.IsNullOrWhiteSpace(setting.Email))
                    {
                        // MassTransit's in-memory outbox delivers the message after the consumer completes and
                        // retries the consumer if the delivery fails: no manual retry loop needed here
                        await context.Publish(new SendMailMessage()
                        {
                            Body = $"<p>The {WebUtility.HtmlEncode(website)} website update file (type {dataType}) is ready to download</p>",
                            Id = Guid.NewGuid(),
                            IsHtmlBody = true,
                            Subject = $"[CMS] Website {subjectWebsite} update file creation complete with success",
                            To = [setting.Email]
                        });
                    }
                }
            }
            else
            {
                var rs = await _publicationService.Update(setting.Id.Value, PublicationStatus.Failed, "Website update failed", null);
                if (!rs.Success)
                {
                    throw new Exception(string.Join(",", rs.Errors.Select(s => s.Description)));
                }

                if (!string.IsNullOrWhiteSpace(setting.Email))
                {
                    var body = new StringBuilder(
                        $"<p>Website {WebUtility.HtmlEncode(website)} update (type {dataType}) is failed!</p><p>The updated was interrupted so run a new website update.These are the errors recorded during the update process:</p>"
                      );
                    foreach (var e in generateRs.Errors)
                    {
                        body.Append($"<p>{WebUtility.HtmlEncode(e.Context)}:{WebUtility.HtmlEncode(e.Description)}</p>");
                    }

                    await context.Publish(new SendMailMessage()
                    {
                        Id = Guid.NewGuid(),
                        IsHtmlBody = true,
                        Subject = $"[CMS] Website {subjectWebsite} update failed",
                        Body = body.ToString(),
                        To = [setting.Email],
                    });
                }
            }
        }
    }
}