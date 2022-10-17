using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
                                string body = "<p>The {0} website update (type {2}) file is ready to download";
                                string fileInfo = string.Format("<a href=\"{0}/Tools/DownloadZip/{1}\">Click here</a> to download the zip file", message.Host, setting.Id);

                                if (!setting.ManualDelete)
                                {
                                    fileInfo += "<p>The file will erase after the download</p>";
                                }

                                body = string.Format(body, website, fileInfo, dataType);

                                SendMailMessage mail = new SendMailMessage()
                                {
                                    Body = body,
                                    Id = Guid.NewGuid(),
                                    IsHtmlBody = true,
                                    Subject = string.Format("[CMS] Website {0} updated completed with success", website),
                                };

                                mail.To.Add(setting.Email);

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
                        var mail = new SendMailMessage()
                        {
                            Id = Guid.NewGuid(),
                            IsHtmlBody = true,
                        };
                        mail.To.Add(setting.Email);
                        mail.Subject = string.Format("[CMS] Website update {0} failed", website);

                        var body = "<p>Website {0} update (type {1}) is failed!</p>";
                        body += "<p>These are the errors recorded during the update process. The updated was interrupted so run a new website update</p>";
                        foreach (var e in generateRs.Errors)
                        {
                            body += "<p>" + e.Context + ":" + e.Description + "</p>";
                        }

                        mail.Body = string.Format(body, website, dataType);
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