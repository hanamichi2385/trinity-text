using System.Collections.Generic;

namespace TrinityText.Domain
{
    public partial class PageType
    {
        public PageType()
        {
            this.PAGES = new HashSet<Page>();
        }
    
        public int ID { get; set; }
        public virtual string NAME { get; set; }
        public virtual string OUTPUT_FILENAME { get; set; }
        public virtual string SCHEMA { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual string SUBFOLDER { get; set; }
        public virtual string PATH_PREVIEWPAGE { get; set; }
        public virtual string PRINT_ELEMENT_NAME { get; set; }
        public virtual string VISIBILITY { get; set; }
    
        public virtual ICollection<Page> PAGES { get; set; }
    }
}
