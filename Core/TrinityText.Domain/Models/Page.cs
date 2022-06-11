using System;

namespace TrinityText.Domain
{
    public partial class Page
    {
        public virtual int ID { get; set; }
        public virtual string TITOLO { get; set; }
        public virtual string CONTENUTO { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual string FK_PRICELIST { get; set; }
        public virtual int FK_PAGETYPE { get; set; }
        public virtual string FK_LINGUA { get; set; }
        public virtual bool GENERA_PDF { get; set; }
        public virtual bool ATTIVA { get; set; }
        public virtual string UTENTE_CREAZIONE { get; set; }
        public virtual string UTENTE_ULTIMA_MODIFICA { get; set; }
        public virtual DateTime DATA_CREAZIONE { get; set; }
        public virtual DateTime DATA_ULTIMO_AGGIORNAMENTO { get; set; }
        public virtual PageType PAGETYPE { get; set; }
    }
}
