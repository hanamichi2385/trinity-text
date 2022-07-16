namespace TrinityText.Domain
{
    public partial class CacheSettings : IEntity
    {
        public virtual int ID { get; set; }
        public virtual int FK_CDNSERVER { get; set; }
        public virtual string TYPE { get; set; }
        public virtual string PAYLOAD { get; set; }
        public virtual CdnServer CDNSERVER { get; set; }
    }
}
