namespace TrinityText.Domain
{
    public partial class FtpServerPerCdnServer
    {
        public virtual int FK_FTPSERVER { get; set; }
        public virtual int FK_CDNSERVER { get; set; }
    
        public virtual CdnServer CDNSERVER { get; set; }

        public virtual FtpServer FTPSERVER { get; set; }
    }
}
