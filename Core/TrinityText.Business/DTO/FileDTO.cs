using System;

namespace TrinityText.Business
{
    public class FileDTO
    {
        public Guid Id { get; set; }

        public string Filename { get; set; }

        public string LastUpdate { get; set; }

        public string CreationUser { get; set; }

        public byte[] Content { get; set; }

        public bool HasThumbnail { get; set; }

        public int Size { get; set; }
    }
}