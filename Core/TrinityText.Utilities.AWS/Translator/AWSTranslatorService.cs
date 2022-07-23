using Amazon;
using Amazon.Translate;
using Amazon.Translate.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities.AWS
{
    public class AWSTranslatorService : ITranslatorService
    {
        private readonly ILogger<AWSTranslatorService> _logger;

        private readonly AWSOptions _options;

        public AWSTranslatorService(IOptions<AWSOptions> options, ILogger<AWSTranslatorService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> TranslateText(string text, string sourceLang, string targetLang)
        {
            var translateText = string.Empty;

            try
            {
                var accessId = _options.AccessId;
                var secretKey = _options.SecretKey;
                var region = _options.Region;
                var cfg = new AmazonTranslateConfig() { RegionEndpoint = RegionEndpoint.EUWest1 };

                using (AmazonTranslateClient cfc = new AmazonTranslateClient(accessId, secretKey, RegionEndpoint.GetBySystemName(region)))
                {
                    var request = new TranslateTextRequest()
                    {
                        SourceLanguageCode = sourceLang,
                        TargetLanguageCode = targetLang,
                        Text = text,
                    };

                    var response = await cfc.TranslateTextAsync(request);

                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        translateText = response.TranslatedText;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AWS.TRANSLATETEXT");
            }

            return translateText;
        }
    }
}
