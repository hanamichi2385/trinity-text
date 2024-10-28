using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TrinityText.Business
{
    public class PublicationDTO
    {
        public int? Id { get; set; }

        public string CreationUser { get; set; }

        public string Email { get; set; }

        public DateTime LastUpdate { get; set; }

        public string StatusMessage { get; set; }

        public string Website { get; set; }

        public FTPServerDTO FtpServer { get; set; }

        public CdnServerDTO CdnServer { get; set; }

        public PublicationType DataType { get; set; }

        public byte[] ZipFile { get; set; }

        public bool HasZipFile { get; set; }

        public bool ManualDelete { get; set; }

        public DateTime FilterDataDate { get; set; }

        public PublicationStatus StatusCode { get; set; }

        public PublicationFormat Format { get; set; }

        public PayloadDTO Payload { get; private set; }

        //public string WebsiteUrl { get; set; }

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

    public enum PublicationFormat
    {
        JSON = 0,
        XML = 1
    }

    public class PayloadDTO
    {
        public SiteConfiguration[] Sites { get; set; }

        public string Website { get; set; }

        public string Tenant { get; set; }

        public TextTypeDTO[] TextTypes { get; set; }
    }

    public class SiteConfiguration
    {
        public string Tenant { get; set; }
        public string Website { get; set; }
        public string Site { get; set; }
        public string CurrencyId { get; set; }
        public string[] Languages { get; set; }
        public string[] Countries { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
    }
}
