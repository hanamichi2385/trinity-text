using System;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public static class ImageExtensions
    {
        // Building the provider fills a ~380-entry mapping table; it is read-only afterwards, so one
        // shared instance is thread-safe and avoids re-allocating it on every call.
        private static readonly Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ContentTypeProvider = new();

        public static string GetMimeTypeForFile(string filePath)
        {
            const string DefaultContentType = "application/octet-stream";

            if (!ContentTypeProvider.TryGetContentType(filePath, out string contentType))
            {
                contentType = DefaultContentType;
            }

            return contentType;
        }

        public static void CheckImageSize(IImageDrawingOptions options, int width, int height, out int newWidth, out int newHeight)
        {
            newWidth = width;
            newHeight = height;

            // an unset (0) limit would make the loops below run forever
            if (options.ThumbWidth <= 0 || options.ThumbHeight <= 0)
            {
                throw new InvalidOperationException("ThumbWidth and ThumbHeight must be greater than zero");
            }

            while (newWidth > options.ThumbWidth)
            {
                decimal percWidth = (decimal)options.ThumbWidth / (decimal)width;

                var pw = (int)(percWidth * width);
                var ph = (int)(percWidth * height);
                newWidth = pw == 0 ? 1 : pw;
                newHeight = ph == 0 ? 1 : ph;
            }

            while (newHeight > options.ThumbHeight)
            {
                decimal percHeight = (decimal)options.ThumbHeight / (decimal)height;

                var pw = (int)(percHeight * width);
                var ph = (int)(percHeight * height);
                newWidth = pw == 0 ? 1 : pw;
                newHeight = ph == 0 ? 1 : ph;
            }
        }

        public static bool IsConvertible(FileDTO dto)
        {
            var contentType = GetMimeTypeForFile(dto.Filename);

            if (contentType.StartsWith("image", StringComparison.InvariantCultureIgnoreCase))
            {
                return contentType.Contains("svg+xml", StringComparison.InvariantCultureIgnoreCase) == false;
            }
            else
            {
                return false;
            }
        } 
    }
}
