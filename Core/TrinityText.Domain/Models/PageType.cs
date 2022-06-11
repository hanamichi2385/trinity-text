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
        public virtual string NOME { get; set; }
        public virtual string OUTPUT_FILENAME { get; set; }
        public virtual string XMLSCHEMA { get; set; }
        public virtual string FK_VENDOR { get; set; }
        public virtual string SUBFOLDER { get; set; }
        public virtual string PATH_PAGINAPREVIEW { get; set; }
        public virtual string PRINT_ELEMENT_NAME { get; set; }
        public virtual string VISIBILITA { get; set; }
    
        public virtual ICollection<Page> PAGES { get; set; }
    }
}
