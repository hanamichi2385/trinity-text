namespace TrinityText.Domain
{
    public partial class CdnServersPerWebsite
    {
        public virtual string FK_WEBSITE { get; set; }
        public virtual int FK_CDNSERVER { get; set; }
    
        public virtual CdnServer CDNSERVER { get; set; }
    }
}
