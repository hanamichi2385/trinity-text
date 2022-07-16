namespace TrinityText.Business
{
    public class FtpServerDTO
    {
        public string Name { get; set; }

        public string Host { get; set; }

        public string Password { get; set; }

        public string Username { get; set; }

        public int? Id { get; set; }

        public int? Port { get; set; }

        public EnvironmentType Type { get; set; }

        public int? IdType { get; set; }
    }
}