using System;

namespace TrinityText.Domain
{
    public partial class Publication
    {
        public virtual int ID { get; set; }
        public virtual string FK_UTENTE { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual int TIPO_ESPORTAZIONE { get; set; }
        public virtual Nullable<int> FK_FTPSERVER { get; set; }
        public virtual System.DateTime DATA_FILTROGENERAZIONE_FILE { get; set; }
        public virtual string STATUS { get; set; }
        public virtual byte[] ZIP_FILE { get; set; }
        public DateTime ULTIMO_AGGIORNAMENTO { get; set; }
        public virtual bool PRESERVACOPIA { get; set; }
        public virtual int STATUS_CODE { get; set; }
        public virtual string WEBSITE { get; set; }
        public virtual Nullable<int> FK_CDNSERVER { get; set; }
        public virtual string PAYLOAD { get; set; }
        public virtual string EMAIL { get; set; }
        public virtual int TYPE { get; set; }
    
        public virtual CdnServer CDNSERVER { get; set; }
        public virtual FtpServer FTPSERVER { get; set; }
    }
}
