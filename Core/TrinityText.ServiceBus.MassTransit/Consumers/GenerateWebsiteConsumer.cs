using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Business.Services;
using TrinityText.ServiceBus.Messages;
using V1_0 = TrinityText.ServiceBus.Messages.V1_0;

namespace TrinityText.ServiceBus.MassTransit.Consumers
{
    public class GenerateWebsiteConsumer : IConsumer<V1_0.IGenerateWebsiteMessage>
    {
        private readonly IGenerationSupportService _generationService;

        private readonly IPublicationService _publicationService;

        private readonly ILogger<GenerateWebsiteConsumer> _logger;

        public GenerateWebsiteConsumer(IGenerationSupportService generationService, IPublicationService publicationService, ILogger<GenerateWebsiteConsumer> logger)
        {
            _generationService = generationService;
            _publicationService = publicationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<V1_0.IGenerateWebsiteMessage> context)
        {
            var message = context.Message;

            var filesGenerationSettingRs = await _publicationService.Get(message.FilesGenerationId);

            if (filesGenerationSettingRs.Success)
            {
                var filesGenerationSetting = filesGenerationSettingRs.Value;
                string operationsLog = await _generationService.Generate(filesGenerationSetting);
                string tipoEsportazione = filesGenerationSetting.PublicationType.ToString();
                string vendorName = filesGenerationSetting.Website;

                //operation log empty = generate with success
                if (string.IsNullOrEmpty(operationsLog))
                {
                    bool includePublishing = filesGenerationSetting.FtpServer != null;
                    if (includePublishing)
                    {
                        filesGenerationSetting.StatusCode = PublicationStatus.Publishing;
                        filesGenerationSetting.Status = "Website update is on the way";
                        var rs = await _publicationService.Save(filesGenerationSetting);

                        if (rs.Success)
                        {
                            var publishmsg = new PublishWebsiteMessage(rs.Value.Id.Value, message.Host);
                            await context.Publish(publishmsg);
                        }
                        else
                        {
                            throw new Exception(string.Join(",", rs.Errors.Select(s => s.Description)));
                        }
                    }
                    else
                    {
                        filesGenerationSetting.StatusCode = PublicationStatus.Success;
                        var rs = await _publicationService.Save(filesGenerationSetting);

                        if (rs.Success)
                        {
                            if (!string.IsNullOrWhiteSpace(filesGenerationSetting.Email))
                            {
                                string body = "<p>The {0} website update (type {2}) file is ready to download";
                                string fileInfo = string.Format("<a href=\"{0}/Tools/DownloadZip/{1}\">Click here</a> to download the zip file", message.Host, filesGenerationSetting.Id);

                                if (!filesGenerationSetting.PreserveCopy)
                                {
                                    fileInfo += "<p>The file will erase after the download</p>";
                                }

                                body = string.Format(body, vendorName, fileInfo, tipoEsportazione);

                                SendMailMessage mail = new SendMailMessage()
                                {
                                    Body = body,
                                    Id = Guid.NewGuid(),
                                    IsHtmlBody = true,
                                    Subject = string.Format("[CMS] Website {0} updated completed with success", vendorName),
                                };

                                mail.To.Add(filesGenerationSetting.Email);

                                int retry = 5;
                                while (retry > 0)
                                {
                                    try
                                    {
                                        await context.Publish(mail);
                                        retry = 0;
                                    }
                                    catch (Exception ex)
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
                    filesGenerationSetting.StatusCode = PublicationStatus.Failed;
                    filesGenerationSetting.Status = "Website update failed";
                    var rs = await _publicationService.Save(filesGenerationSetting);

                    if (rs.Success)
                    {
                        if (!string.IsNullOrEmpty(operationsLog))
                        {
                            var logs = operationsLog.Replace(System.Environment.NewLine, "<br/>");

                            SendMailMessage mail = new SendMailMessage()
                            {
                                Id = Guid.NewGuid(),
                                IsHtmlBody = true,
                            };
                            mail.To.Add(filesGenerationSetting.Email);
                            mail.Subject = string.Format("[CMS] Website update {0} failed", vendorName);

                            string body = "<p>Website {0} update (type {1}) is failed!</p>";
                            body += "<p>These are the errors recorded during the update process. The updated was interrupted so run a new website update</p>";
                            body += "<p>" + logs + "</p>";

                            mail.Body = string.Format(body, vendorName, tipoEsportazione);

                            int retry = 5;
                            while (retry > 0)
                            {
                                try
                                {
                                    await context.Publish(mail);
                                    retry = 0;
                                }
                                catch (Exception ex)
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
                throw new Exception(string.Join(",", filesGenerationSettingRs.Errors.Select(s => s.Description)));
            }
        }
    }
}