using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resulz;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public class ImageDrawingService : IImageDrawingService
    {
        private readonly ImageDrawingOptions _options;

        private readonly ILogger<ImageDrawingService> _logger;

        public ImageDrawingService(IOptions<ImageDrawingOptions> options, ILogger<ImageDrawingService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<OperationResult<byte[]>> GenerateThumb(FileDTO dto)
        {
            try
            {
                var bytes = default(byte[]);

                var contentType = ImageExtensions.GetMimeTypeForFile(dto.Filename); ;
                //image/svg+xml è per i font ma non è una immagine
                if (contentType.StartsWith("image", StringComparison.InvariantCultureIgnoreCase) && !contentType.Equals("image/svg+xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var stream = new MemoryStream(dto.Content))
                    {
                        var img = Image.FromStream(stream);
                        int w = 0;
                        int h = 0;

                        ImageExtensions.CheckImageSize(_options, img.Width, img.Height, out w, out h);

                        var thumb = img.GetThumbnailImage(w, h, () => { return false; }, IntPtr.Zero);

                        using (MemoryStream thumbStream = new MemoryStream())
                        {
                            var imgFormat = img.RawFormat as System.Drawing.Imaging.ImageFormat;
                            thumb.Save(thumbStream, imgFormat);

                            bytes = thumbStream.GetBuffer();

                            thumbStream.Close();
                        }
                        stream.Close();
                    }
                }

                return await Task.FromResult(OperationResult<byte[]>.MakeSuccess(bytes));
            }
            catch (Exception ex)
            {
                _logger.LogError("GENERATE_THUMB", ex);
                return OperationResult<byte[]>.MakeFailure(new[] { ErrorMessage.Create("GENERATE_THUMB", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<byte[]>> Compression(FileDTO dto)
        {
            return await Task.FromResult(OperationResult<byte[]>.MakeFailure(new[] { ErrorMessage.Create("COMPRESSION", "NOT_SUPPORTED") }));
        }
    }
}
