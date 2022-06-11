using System;

namespace TrinityText.Domain
{
    public partial class File
    {
        public virtual Guid ID { get; set; }
        public virtual string FILENAME { get; set; }
        public virtual byte[] CONTENT { get; set; }
        public virtual byte[] THUMBNAIL { get; set; }
        public virtual DateTime DATA_ULTIMA_MODIFICA { get; set; }
        public virtual string UTENTE_CREAZIONE { get; set; }
        public virtual int FK_FOLDER { get; set; }
        public virtual string FK_VENDOR { get; set; }
        public virtual DateTime DATA_CREAZIONE { get; set; }
        public virtual string UTENTE_ULTIMA_MODIFICA { get; set; }
    
        public virtual Folder FOLDER { get; set; }
    }
}
