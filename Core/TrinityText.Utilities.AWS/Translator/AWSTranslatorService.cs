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
    public class AWSTranslatorService : ITranslatorService, IDisposable
    {
        private readonly ILogger<AWSTranslatorService> _logger;

        private readonly AWSOptions _options;

        // one client per service instance (a new HTTP stack + handshake for every text was expensive)
        private readonly Lazy<AmazonTranslateClient> _client;

        public AWSTranslatorService(IOptions<AWSOptions> options, ILogger<AWSTranslatorService> logger)
        {
            _options = options.Value;
            _logger = logger;
            _client = new Lazy<AmazonTranslateClient>(() =>
                new AmazonTranslateClient(_options.AccessId, _options.SecretKey, RegionEndpoint.GetBySystemName(_options.Region)));
        }

        public async Task<string> TranslateText(string text, string sourceLang, string targetLang)
        {
            try
            {
                var request = new TranslateTextRequest()
                {
                    SourceLanguageCode = sourceLang,
                    TargetLanguageCode = targetLang,
                    Text = text,
                };

                var response = await _client.Value.TranslateTextAsync(request);

                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new InvalidOperationException($"AWS Translate returned {response.HttpStatusCode}");
                }

                return response.TranslatedText;
            }
            catch (Exception ex)
            {
                // an empty string used to be returned here and could be saved as "the translation"
                _logger.LogError(ex, "AWS.TRANSLATETEXT");
                throw;
            }
        }

        public void Dispose()
        {
            if (_client.IsValueCreated)
            {
                _client.Value.Dispose();
            }
        }
    }
}
