﻿using System.Text.Json;

namespace TrinityText.Business
{
    public class CacheSettingsDTO
    {
        public int? Id { get; set; }

        public string Type { get; set; }

        public string Payload { get; set; }

        public int? CdnServerId { get; set; }

        public CdnServerDTO CdnServer { get; set; }

        public T ObtainPayload<T>() where T : ICacheSettingPayload
        {
            return JsonSerializer.Deserialize<T>(Payload);
        }

        public bool Validate<T>() where T : ICacheSettingPayload
        {
            try
            {
                if (!string.IsNullOrEmpty(Payload))
                {
                    var obj = ObtainPayload<T>();
                    return obj.Validate();
                }
                else
                {
                    return false;
                }
            }catch
            {
                return false;
            }
        }
    }
    
    public interface ICacheSettingPayload
    {
        bool Validate();
    }
}
