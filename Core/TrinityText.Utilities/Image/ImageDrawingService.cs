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

                var contentType = GetMimeTypeForFile(dto.Filename); ;
                //image/svg+xml è per i font ma non è una immagine
                if (contentType.StartsWith("image", StringComparison.InvariantCultureIgnoreCase) && !contentType.Equals("image/svg+xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var stream = new MemoryStream(dto.Content))
                    {
                        var img = Image.FromStream(stream);
                        int w = 0;
                        int h = 0;

                        CheckImageSize(img.Width, img.Height, out w, out h);

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

        private string GetMimeTypeForFile(string filePath)
        {
            const string DefaultContentType = "application/octet-stream";

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(filePath, out string contentType))
            {
                contentType = DefaultContentType;
            }

            return contentType;
        }

        private void CheckImageSize(int width, int height, out int newWidth, out int newHeight)
        {
            newWidth = width;
            newHeight = height;

            while (newWidth > _options.ThumbWidth)
            {
                decimal percWidth = (decimal)_options.ThumbWidth / (decimal)width;

                var pw = (int)(percWidth * width);
                var ph = (int)(percWidth * height);
                newWidth = pw == 0 ? 1 : pw;
                newHeight = ph == 0 ? 1 : ph;
            }

            while (newHeight > _options.ThumbHeight)
            {
                decimal percHeight = (decimal)_options.ThumbHeight / (decimal)height;

                var pw = (int)(percHeight * width);
                var ph = (int)(percHeight * height);
                newWidth = pw == 0 ? 1 : pw;
                newHeight = ph == 0 ? 1 : ph;
            }
        }
    }
}
