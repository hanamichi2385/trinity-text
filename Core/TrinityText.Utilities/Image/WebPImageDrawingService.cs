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
                    // one codec per operation: header checks (size limit, animation) and the decode share it
                    using var codec = SKCodec.Create(new SKMemoryStream(dto.Content));
                    if (codec != null && CanProcess(dto.Filename, codec) && WithinPixelLimit(codec))
                    {
                        var source = codec.Info;
                        ImageExtensions.CheckImageSize(_options, source.Width, source.Height, out int w, out int h);

                        // decode straight at a reduced size when the format supports it (JPEG / WebP): far less
                        // work and memory than decoding the full bitmap and shrinking it afterwards
                        var scale = Math.Min(1f, Math.Max(w / (float)source.Width, h / (float)source.Height));
                        var decodeSize = codec.GetScaledDimensions(scale);
                        using var decoded = SKBitmap.Decode(codec, CreateDecodeInfo(source, decodeSize.Width, decodeSize.Height));
                        if (decoded != null)
                        {
                            if (decoded.Width == w && decoded.Height == h)
                            {
                                using var data = decoded.Encode(SKEncodedImageFormat.Webp, _options.Quality);
                                bytes = data.ToArray();
                            }
                            else
                            {
                                var info = new SKImageInfo(w, h, decoded.ColorType, decoded.AlphaType, decoded.Info.ColorSpace);
                                using var thumb = decoded.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                                if (thumb != null)
                                {
                                    using var data = thumb.Encode(SKEncodedImageFormat.Webp, _options.Quality);
                                    bytes = data.ToArray();
                                }
                            }
                        }
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
                    using var codec = SKCodec.Create(new SKMemoryStream(dto.Content));
                    if (codec != null && CanProcess(dto.Filename, codec) && WithinPixelLimit(codec))
                    {
                        var source = codec.Info;
                        using var original = SKBitmap.Decode(codec, CreateDecodeInfo(source, source.Width, source.Height));
                        if (original != null)
                        {
                            using var data = original.Encode(SKEncodedImageFormat.Webp, _options.Quality);
                            bytes = data.ToArray();
                        }
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

        // same color type / alpha handling as SKBitmap.Decode(SKCodec), with a custom size
        private static SKImageInfo CreateDecodeInfo(SKImageInfo source, int width, int height)
            => new(width, height, SKImageInfo.PlatformColorType,
                source.AlphaType == SKAlphaType.Unpremul ? SKAlphaType.Premul : source.AlphaType,
                source.ColorSpace);

        private static bool CanProcess(string filename, SKCodec codec)
        {
            var contentType = ImageExtensions.GetMimeTypeForFile(filename);
            if ("image/gif".Equals(contentType, StringComparison.InvariantCultureIgnoreCase))
            {
                // animated GIFs are left untouched
                return codec.FrameCount <= 1;
            }
            return true;
        }

        // reads only the header: a tiny file can declare a huge canvas
        private bool WithinPixelLimit(SKCodec codec)
        {
            var pixels = (long)codec.Info.Width * codec.Info.Height;
            if (pixels > _options.MaxPixels)
            {
                _logger.LogWarning("Image skipped: {pixels} pixels exceed the limit of {max}", pixels, _options.MaxPixels);
                return false;
            }

            return true;
        }
    }
}
