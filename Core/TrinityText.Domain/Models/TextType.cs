using System.Collections.Generic;

namespace TrinityText.Domain
{
    public partial class TextType
    {
        public TextType()
        {
            this.TEXTS = new HashSet<Text>();
            this.TEXTTYPEPERWEBSITES = new HashSet<TextTypePerWebsite>();
        }
    
        public virtual int ID { get; set; }
        public virtual string TIPOLOGIA { get; set; }
        public virtual string NOTE { get; set; }
        public virtual string SUBFOLDER { get; set; }
    
        public virtual ICollection<Text> TEXTS { get; set; }
        public virtual ICollection<TextTypePerWebsite> TEXTTYPEPERWEBSITES { get; set; }
    }
}
