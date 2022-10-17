using System;

namespace TrinityText.Domain
{
    public partial class Publication : IEntity
    {
        public virtual int ID { get; set; }
        public virtual string CREATION_USER { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual int DATATYPE { get; set; }
        public virtual int? FK_FTPSERVER { get; set; }
        public virtual System.DateTime FILTERDATA_DATE { get; set; }
        public virtual string STATUS_MESSAGE { get; set; }
        public virtual byte[] ZIP_FILE { get; set; }
        public DateTime LASTUPDATE_DATE { get; set; }
        public virtual bool MANUALDELETE { get; set; }
        public virtual int STATUS_CODE { get; set; }
        //public virtual string WEBSITE { get; set; }
        public virtual int? FK_CDNSERVER { get; set; }
        public virtual string PAYLOAD { get; set; }
        public virtual string EMAIL { get; set; }
        public virtual int FORMAT { get; set; }
    
        public virtual CdnServer CDNSERVER { get; set; }
        public virtual FtpServer FTPSERVER { get; set; }
    }
}
