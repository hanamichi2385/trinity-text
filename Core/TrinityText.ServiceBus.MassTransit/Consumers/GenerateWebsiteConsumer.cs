using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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

            if (publicationSettingRs.Success)
            {
                var setting = publicationSettingRs.Value;
                await _publicationService.Update(setting.Id.Value, PublicationStatus.Generating, "Generation started", null);
                var generateRs = await _generationService.Generate(setting);
                string dataType = setting.DataType.ToString();
                string website = setting.Website;

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
                        if (rs.Success)
                        {
                            if (!string.IsNullOrWhiteSpace(setting.Email))
                            {
                                var mail = new SendMailMessage()
                                {
                                    Body = $"<p>The {website} website update file (type {dataType}) is ready to download",
                                    Id = Guid.NewGuid(),
                                    IsHtmlBody = true,
                                    Subject = $"[CMS] Website {website} updated completed with success",
                                    To = [setting.Email]
                                };

                                int retry = 5;
                                while (retry > 0)
                                {
                                    try
                                    {
                                        await context.Publish(mail);
                                        retry = 0;
                                    }
                                    catch
                                    {
                                        retry -= 1;
                                    }
                                };
                            }
                        }
                        else
                        {
                            throw new Exception(string.Join(",", rs.Errors.Select(s => s.Description)));
                        }
                    }
                }
                else
                {
                    var rs = await _publicationService.Update(setting.Id.Value, PublicationStatus.Failed, "Website update failed", null);
                    if (rs.Success)
                    {
                        var body = new StringBuilder(
                            $"<p>Website {website} update (type {dataType}) is failed!</p><p>These are the errors recorded during the update process. The updated was interrupted so run a new website update</p>"
                          );
                        foreach (var e in generateRs.Errors)
                        {
                            body.Append($"<p>{e.Context}:{e.Description}</p>");
                        }
                        var mail = new SendMailMessage()
                        {
                            Id = Guid.NewGuid(),
                            IsHtmlBody = true,
                            Subject = $"[CMS] Website {website} update failed",
                            Body = body.ToString(),
                        };
                        mail.To.Add(setting.Email);
                        await context.Publish(mail);
                    }
                    else
                    {
                        throw new Exception(string.Join(",", rs.Errors.Select(s => s.Description)));
                    }
                }
            }
            else
            {
                throw new Exception(string.Join(",", publicationSettingRs.Errors.Select(s => s.Description)));
            }
        }
    }
}