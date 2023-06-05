namespace TrinityText.Domain
{
    
    public partial class TextTypePerWebsite : IEntity
    {
        public virtual string FK_WEBSITE { get; set; }
        public virtual int FK_TEXTTYPE { get; set; }
    
        public virtual TextType TEXTTYPE { get; set; }

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
