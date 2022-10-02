using System.Collections.Generic;

namespace TrinityText.Business
{
    public class CdnServerDTO
    {
        public int? Id { get; set; }

        public string Name { get; set; }

        public string BaseUrl { get; set; }

        public int? IdType { get; set; }

        public EnvironmentType Type { get; set; }

        public IList<FTPServerDTO> FtpServers { get; set; }
    }
}