using System;

namespace TrinityText.Business
{
    public class WidgetDTO
    {
        public int? Id { get;  set; }
        
        public string Key { get; set; }

        public string Content { get; set; }

        public string Website { get; set; }

        public string Site { get; set; }

        public string Language { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime LastUpdate { get; set; }

        public string CreationUser { get; set; }

        public string LastUpdateUser { get; set; }
    }
}