namespace TrinityText.Business.Schema
{ 
    public class Particol
    {
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Path);
            }
        }

        public string Path { get; set; }

        public string Caption { get; set; }

        public string Link { get; set; }
    }
}