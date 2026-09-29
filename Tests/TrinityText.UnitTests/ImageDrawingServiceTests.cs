using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Utilities;

namespace TrinityText.UnitTests
{
    /// <summary>Image processing checks: no database needed.</summary>
    [TestClass]
    [TestCategory("Offline")]
    public class ImageDrawingServiceTests
    {
        private static WebPImageDrawingService CreateService(long maxPixels = 50_000_000)
            => new(
                Options.Create(new WebPImageDrawingOptions { ThumbWidth = 200, ThumbHeight = 200, Quality = 75, MaxPixels = maxPixels }),
                NullLogger<WebPImageDrawingService>.Instance);

        private static byte[] CreateImage(int width, int height, SKEncodedImageFormat format)
        {
            using var bitmap = new SKBitmap(width, height);
            var random = new Random(42);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    bitmap.SetPixel(x, y, new SKColor((byte)random.Next(256), (byte)(x % 256), (byte)(y % 256)));
                }
            }

            using var data = bitmap.Encode(format, 90);
            return data.ToArray();
        }

        private static (int Width, int Height) Measure(byte[] content)
        {
            using var bitmap = SKBitmap.Decode(content);
            return (bitmap.Width, bitmap.Height);
        }

        [TestMethod]
        public async Task GenerateThumb_Jpeg_FitsBoundsKeepingAspectRatio()
        {
            var dto = new FileDTO { Filename = "photo.jpg", Content = CreateImage(2000, 1000, SKEncodedImageFormat.Jpeg) };

            var rs = await CreateService().GenerateThumb(dto);

            Assert.IsTrue(rs.Success);
            var (w, h) = Measure(rs.Value);
            Assert.IsTrue(w <= 200 && h <= 200, $"thumb {w}x{h} exceeds the bounds");
            Assert.AreEqual(2.0, Math.Round((double)w / h, 1));
        }

        [TestMethod]
        public async Task GenerateThumb_Png_FitsBounds()
        {
            var dto = new FileDTO { Filename = "photo.png", Content = CreateImage(800, 800, SKEncodedImageFormat.Png) };

            var rs = await CreateService().GenerateThumb(dto);

            Assert.IsTrue(rs.Success);
            var (w, h) = Measure(rs.Value);
            Assert.IsTrue(w <= 200 && h <= 200, $"thumb {w}x{h} exceeds the bounds");
        }

        [TestMethod]
        public async Task Compression_KeepsDimensions()
        {
            var dto = new FileDTO { Filename = "photo.png", Content = CreateImage(300, 200, SKEncodedImageFormat.Png) };

            var rs = await CreateService().Compression(dto);

            Assert.IsTrue(rs.Success);
            Assert.AreEqual((300, 200), Measure(rs.Value));
        }

        [TestMethod]
        public async Task ImageOverPixelLimit_IsNotDecoded()
        {
            var dto = new FileDTO { Filename = "photo.png", Content = CreateImage(100, 100, SKEncodedImageFormat.Png) };
            var service = CreateService(maxPixels: 1000);

            var thumb = await service.GenerateThumb(dto);
            var compression = await service.Compression(dto);

            Assert.IsFalse(thumb.Success);
            Assert.IsFalse(compression.Success);
        }

        [TestMethod]
        public async Task NotAnImage_IsSkipped()
        {
            var dto = new FileDTO { Filename = "photo.png", Content = [1, 2, 3, 4] };

            var rs = await CreateService().GenerateThumb(dto);

            Assert.IsFalse(rs.Success);
        }
    }
}
