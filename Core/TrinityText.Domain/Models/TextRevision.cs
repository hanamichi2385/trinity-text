using System;

namespace TrinityText.Domain
{
    
    public partial class TextRevision
    {
        public virtual int ID { get; set; }
        public virtual int FK_TEXT { get; set; }
        public virtual string TESTO { get; set; }
        public virtual int REVISIONE { get; set; }
        public virtual DateTime DATA_CREAZIONE { get; set; }
        public virtual string UTENTE_CREAZIONE { get; set; }
    
        public virtual Text TEXT { get; set; }
    }
}
