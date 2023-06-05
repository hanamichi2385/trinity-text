using System;

namespace TrinityText.Domain
{
  
    public partial class Widget : IEntity
    {
        public virtual int ID { get; set; }
        public virtual string KEY { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual string FK_PRICELIST { get; set; }
        public virtual string FK_LANGUAGE { get; set; }
        public virtual string CONTENT { get; set; }
        public virtual string CREATION_USER { get; set; }
        public virtual string LASTUPDATE_USER { get; set; }
        public virtual DateTime CREATION_DATE { get; set; }
        public virtual DateTime LASTUPDATE_DATE { get; set; }
    }
}
