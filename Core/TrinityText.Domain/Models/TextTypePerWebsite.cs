namespace TrinityText.Domain
{
    
    public partial class TextTypePerWebsite
    {
        public virtual string FK_WEBSITE { get; set; }
        public virtual int FK_TEXTTYPE { get; set; }
    
        public virtual TextType TEXTTYPE { get; set; }
    }
}
