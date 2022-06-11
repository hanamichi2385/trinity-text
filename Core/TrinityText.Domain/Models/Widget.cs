namespace TrinityText.Domain
{
  
    public partial class Widget
    {
        public virtual int ID { get; set; }
        public virtual string CHIAVE { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual string FK_PRICELIST { get; set; }
        public virtual string FK_CULTURE { get; set; }
        public virtual string CONTENUTO { get; set; }
        public virtual string UTENTE_CREAZIONE { get; set; }
        public virtual string UTENTE_ULTIMA_MODIFICA { get; set; }
        public System.DateTime DATA_CREAZIONE { get; set; }
        public System.DateTime DATA_ULTIMA_MODIFICA { get; set; }
    }
}
