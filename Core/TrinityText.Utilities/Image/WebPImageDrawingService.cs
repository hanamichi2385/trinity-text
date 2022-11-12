using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resulz;
using SkiaSharp;
using System;
using System.Drawing;
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

        public async Task<OperationResult<byte[]>> GenerateThumb(FileDTO dto)
        {
            try
            {
                var bytes = default(byte[]);
                var contentType = ImageExtensions.GetMimeTypeForFile(dto.Filename);
                //image/svg+xml è per i font ma non è una immagine
                if (contentType.StartsWith("image", StringComparison.InvariantCultureIgnoreCase) && !contentType.Equals("image/svg+xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var fileStream = new MemoryStream(dto.Content))
                    {
                        using (var outputFile = new MemoryStream())
                        {
                            using (var inputFile = new MemoryStream(dto.Content))
                            using (var inputStream = new SKManagedStream(inputFile))
                            {
                                using (var original = SKBitmap.Decode(inputStream))
                                {
                                    int w = 0;
                                    int h = 0;

                                    ImageExtensions.CheckImageSize(_options, original.Width, original.Height, out w, out h);


                                    var info = new SKImageInfo() { Width = w, Height = h, ColorType = original.ColorType, AlphaType = original.AlphaType, ColorSpace = original.ColorSpace };
                                    var thumb = original.Resize(info, SKFilterQuality.Medium);

                                    thumb.Encode(SKEncodedImageFormat.Webp, _options.Quality)
                                            .SaveTo(outputFile);
                                }

                            }
                            bytes = outputFile.GetBuffer();

                            outputFile.Close();
                        }
                    }
                }

                if(bytes != null && (bytes.Length < dto.Content.Length))
                {
                    return await Task.FromResult(OperationResult<byte[]>.MakeSuccess(bytes));
                }
                else
                {
                    return OperationResult<byte[]>.MakeFailure(new[] { ErrorMessage.Create("GENERATE_THUMB", "NOT_OPTIMIZED") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GENERATE_THUMB", ex);
                return OperationResult<byte[]>.MakeFailure(new[] { ErrorMessage.Create("GENERATE_THUMB", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<byte[]>> Compression(FileDTO dto)
        {
            try
            {
                var bytes = default(byte[]);
                var contentType = ImageExtensions.GetMimeTypeForFile(dto.Filename);
                //image/svg+xml è per i font ma non è una immagine
                if (contentType.StartsWith("image", StringComparison.InvariantCultureIgnoreCase) && !contentType.Equals("image/svg+xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var outputFile = new MemoryStream())
                    {
                        using (var inputFile = new MemoryStream(dto.Content))
                        using (var inputStream = new SKManagedStream(inputFile))
                        {
                            using (var original = SKBitmap.Decode(inputStream))
                            {
                                original.Encode(SKEncodedImageFormat.Webp, _options.Quality)
                                        .SaveTo(outputFile);

                            }
                        }
                        bytes = outputFile.GetBuffer();

                        outputFile.Close();
                    }
                }
                if (bytes != null && (bytes.Length < dto.Content.Length))
                {
                    return await Task.FromResult(OperationResult<byte[]>.MakeSuccess(bytes));
                }
                else
                {
                    return OperationResult<byte[]>.MakeFailure(new[] { ErrorMessage.Create("COMPRESSION", "NOT_OPTIMIZED") });
                }
                //return await Task.FromResult(OperationResult<byte[]>.MakeSuccess(bytes));
            }
            catch (Exception ex)
            {
                _logger.LogError("COMPRESSION", ex);
                return OperationResult<byte[]>.MakeFailure(new[] { ErrorMessage.Create("COMPRESSION", "GENERIC_ERROR") });
            }
        }

        
    }
}
