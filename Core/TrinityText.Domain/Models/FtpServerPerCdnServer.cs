namespace TrinityText.Domain
{
    public partial class FtpServerPerCdnServer : IEntity
    {
        public virtual int FK_FTPSERVER { get; set; }
        public virtual int FK_CDNSERVER { get; set; }
    
        public virtual CdnServer CDNSERVER { get; set; }

        public virtual FtpServer FTPSERVER { get; set; }
        //TODO
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
