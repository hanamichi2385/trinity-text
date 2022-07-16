using System;

namespace TrinityText.Domain
{
    
    public partial class TextRevision : IEntity
    {
        public virtual int ID { get; set; }
        public virtual int FK_TEXT { get; set; }
        public virtual string CONTENT { get; set; }
        public virtual int REVISION_NUMBER { get; set; }
        public virtual DateTime CREATION_DATE { get; set; }
        public virtual string CREATION_USER { get; set; }
    
        public virtual Text TEXT { get; set; }
    }
}
