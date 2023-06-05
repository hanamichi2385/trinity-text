namespace TrinityText.Domain
{
    public partial class CdnServersPerWebsite : IEntity
    {
        public virtual string FK_WEBSITE { get; set; }
        public virtual int FK_CDNSERVER { get; set; }
    
        public virtual CdnServer CDNSERVER { get; set; }

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
