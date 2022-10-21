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
    }
}
