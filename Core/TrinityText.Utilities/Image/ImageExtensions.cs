﻿using System;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public static class ImageExtensions
    {
        public static string GetMimeTypeForFile(string filePath)
        {
            const string DefaultContentType = "application/octet-stream";

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(filePath, out string contentType))
            {
                contentType = DefaultContentType;
            }

            return contentType;
        }

        public static void CheckImageSize(IImageDrawingOptions options, int width, int height, out int newWidth, out int newHeight)
        {
            newWidth = width;
            newHeight = height;

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
