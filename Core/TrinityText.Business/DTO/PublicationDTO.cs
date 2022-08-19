using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TrinityText.Business
{
    public class PublicationDTO
    {
        public int? Id { get; set; }

        public string Utente { get; set; }

        public string Email { get; set; }

        public DateTime LastUpdate { get; set; }

        public string Status { get; set; }

        public string Website { get; set; }

        public FtpServerDTO FtpServer { get; set; }

        public CdnServerDTO CdnServer { get; set; }

        public PublicationType PublicationType { get; set; }

        public byte[] ZipFile { get; set; }

        public bool HasZipFile { get; set; }

        public bool PreserveCopy { get; set; }

        public DateTime FilterDataDate { get; set; }

        public PublicationStatus StatusCode { get; set; }

        public PublishType PublishType { get; set; }

        public PayloadDTO Payload { get; private set; }

        public string WebsiteUrl { get; set; }

        public string GetPayload()
        {
            var payload = JsonSerializer.Serialize(Payload);
            return payload;
        }

        public void SetPayload(string payload)
        {
            var ss = JsonSerializer.Deserialize<PayloadDTO>(payload);
            this.Payload= ss;
        }

        public void SetPayload(PayloadDTO payload)
        {
            this.Payload = payload;
        }
    }

    public enum PublishType
    {
        JSON = 0,
        XML = 1
    }

    public class PayloadDTO
    {
        public IList<SiteConfiguration> Sites { get; set; }

        public string Website { get; set; }

        public string Tenant { get; set; }

        public IList<TextTypeDTO> TextTypes { get; set; }
    }

    public class SiteConfiguration
    {
        public string Tenant { get; set; }
        public string Website { get; set; }
        public string Site { get; set; }
        public string CurrencyId { get; set; }
        public IList<string> Languages { get; set; }
        public IList<string> Countries { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
    }
}
