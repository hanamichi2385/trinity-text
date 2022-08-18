using System;

namespace TrinityText.Domain
{
    public partial class File : IEntity
    {
        public virtual Guid ID { get; set; }
        public virtual string FILENAME { get; set; }
        public virtual byte[] CONTENT { get; set; }
        public virtual byte[] THUMBNAIL { get; set; }
        public virtual DateTime LASTUPDATE_DATE { get; set; }
        public virtual string CREATION_USER { get; set; }
        public virtual int FK_FOLDER { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual DateTime CREATION_DATE { get; set; }
        public virtual string LASTUPDATE_USER { get; set; }
    
        public virtual Folder FOLDER { get; set; }
    }
}
