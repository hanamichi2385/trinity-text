namespace TrinityText.Business
{
    public class WebsiteConfigurationDTO
    {
        public int? Id { get; set; }

        public string Website { get; set; }

        public int? IdType { get; set; }

        public EnvironmentType Type { get; set; }

        public string Url { get; set; }

        public string Note { get; set; }


    }
}
