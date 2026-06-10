using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resulz;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public class WebPImageDrawingService : IImageDrawingService
    {
        private readonly WebPImageDrawingOptions _options;

        private readonly ILogger<WebPImageDrawingService> _logger;

        public WebPImageDrawingService(IOptions<WebPImageDrawingOptions> options, ILogger<WebPImageDrawingService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Task<OperationResult<byte[]>> GenerateThumb(FileDTO dto)
        {
            try
            {
                var bytes = default(byte[]);
                if (ImageExtensions.IsConvertible(dto))
                {
                    using var inputFile = new MemoryStream(dto.Content);
                    using var inputStream = new SKManagedStream(inputFile);
                    if (CanProcess(dto.Filename, inputStream))
                    {
                        using var original = SKBitmap.Decode(inputStream);
                        ImageExtensions.CheckImageSize(_options, original.Width, original.Height, out int w, out int h);

                        var info = new SKImageInfo() { Width = w, Height = h, ColorType = original.ColorType, AlphaType = original.AlphaType, ColorSpace = original.ColorSpace };

                        using var thumb = original.Resize(info, SKSamplingOptions.Default);
                        using var data = thumb.Encode(SKEncodedImageFormat.Webp, _options.Quality);
                        bytes = data.ToArray();
                    }
                }

                if (bytes != null && (bytes.Length < dto.Content.Length))
                {
                    return Task.FromResult(OperationResult<byte[]>.MakeSuccess(bytes));
                }
                else
                {
                    return Task.FromResult(OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("GENERATE_THUMB", "NOT_OPTIMIZED")]));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GENERATE_THUMB {message}", ex.Message);
                return Task.FromResult(OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("GENERATE_THUMB", "GENERIC_ERROR")]));
            }
        }

        public Task<OperationResult<byte[]>> Compression(FileDTO dto)
        {
            try
            {
                var bytes = default(byte[]);
                if (ImageExtensions.IsConvertible(dto))
                {
                    using var inputFile = new MemoryStream(dto.Content);
                    using var inputStream = new SKManagedStream(inputFile);
                    if (CanProcess(dto.Filename, inputStream))
                    {
                        using var original = SKBitmap.Decode(inputStream);
                        using var data = original.Encode(SKEncodedImageFormat.Webp, _options.Quality);
                        bytes = data.ToArray();
                    }
                }

                if (bytes != null && (bytes.Length < dto.Content.Length))
                {
                    return Task.FromResult(OperationResult<byte[]>.MakeSuccess(bytes));
                }
                else
                {
                    return Task.FromResult(OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("COMPRESSION", "NOT_OPTIMIZED")]));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "COMPRESSION {message}", ex.Message);
                return Task.FromResult(OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("COMPRESSION", "GENERIC_ERROR")]));
            }
        }

        private static bool CanProcess(string filename, SKManagedStream stream)
        {
            var contentType = ImageExtensions.GetMimeTypeForFile(filename);
            if ("image/gif".Equals(contentType, StringComparison.InvariantCultureIgnoreCase))
            {
                using var codec = SKCodec.Create(stream);
                return codec.FrameCount <= 1;
            }
            return true;
        }
    }
}
