using Jdenticon;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public class AvatarGeneratorService : IAvatarGeneratorService
    {
        private readonly ILogger<AvatarGeneratorService> _logger;

        private readonly IMemoryCache _memoryCache;

        public AvatarGeneratorService(IMemoryCache memoryCache, ILogger<AvatarGeneratorService> logger)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<byte[]> Generate(string email)
        {
            try
            {
                var cacheKey = $"avatar.{email}";

                var cache = _memoryCache.Get(cacheKey);

                if (cache != null)
                {
                    var b = cache as byte[];

                    return b;
                }
                else
                {
                    
                    var bytes = new byte[0];
                    using (var ms = new MemoryStream())
                    {
                        await Identicon
                            .FromValue(email, size: 100)
                            .SaveAsPngAsync(ms);
                        
                        bytes = ms.ToArray();
                        ms.Close();
                    }

                    _memoryCache.Set(cacheKey, bytes, TimeSpan.FromDays(1));

                    return await Task.FromResult(bytes);
                }

                
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "AVATARGENERATOR");
            }
            return new byte[0];
        }
    }
}
