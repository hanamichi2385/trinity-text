using System;

namespace TrinityText.Business
{
    public class PageDTO
    {
        public int? Id { get; set; }
        public string Language { get; set; }
        public string Website { get; set; }
        public string Site { get; set; }
        public string Title { get; set; }
        public DateTime? LastUpdate { get; set; }
        public DateTime? CreationDate { get; set; }
        public string CreationUser { get; set; }
        public string LastUpdateUser { get; set; }
        public string Content { get; set; }
        public int PageTypeId { get; set; }
        public PageTypeDTO PageType { get; set; }
        public bool Active { get; set; }
        public bool GeneratePdf { get; set; }
    }
}