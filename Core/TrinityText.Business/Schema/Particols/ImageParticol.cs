namespace TrinityText.Business.Schema
{ 
    public class ImageParticol : IParticol
    {
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Path);
            }
        }

        public string Path { get; set; }

        public string Caption { get; set; }

        public string Link { get; set; }

        public int? Order { get; set; }
    }
}