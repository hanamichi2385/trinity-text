namespace TrinityText.Utilities
{
    public class ImageDrawingOptions : IImageDrawingOptions
    {
        public int ThumbWidth { get; set; }

        public int ThumbHeight { get; set; }
    }

    public class WebPImageDrawingOptions : IImageDrawingOptions
    {
        public int ThumbWidth { get; set; }

        public int ThumbHeight { get; set; }

        public int Quality { get; set; }

        /// <summary>Images declaring more pixels than this are not decoded (decompression-bomb guard). Default 50 megapixels.</summary>
        public long MaxPixels { get; set; } = 50_000_000;
    }
}
