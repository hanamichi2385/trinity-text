using System;

namespace TrinityText.Domain
{
    public partial class Page
    {
        public virtual int ID { get; set; }
        public virtual string TITLE { get; set; }
        public virtual string CONTENT { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual string FK_PRICELIST { get; set; }
        public virtual int FK_PAGETYPE { get; set; }
        public virtual string FK_LINGUAGE { get; set; }
        public virtual bool GENERATE_PDF { get; set; }
        public virtual bool ACTIVE { get; set; }
        public virtual string CREATION_USER { get; set; }
        public virtual string LASTUPDATE_USER { get; set; }
        public virtual DateTime CREATION_DATE { get; set; }
        public virtual DateTime LASTUPDATE_DATE { get; set; }
        public virtual PageType PAGETYPE { get; set; }
    }
}
