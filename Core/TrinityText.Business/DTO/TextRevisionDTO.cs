using System;

namespace TrinityText.Business
{
    public class TextRevisionDTO
    {
        public int? Id { get; set; }

        public int Index { get; set; }

        public string Content { get; set; }

        public DateTime CreationDate { get; set; }

        public string CreationUser { get; set; }
    }

}