using System;
using System.Collections.Generic;

namespace TrinityText.Domain
{
    public partial class Text
    {
        public Text()
        {
            this.REVISIONS = new HashSet<TextRevision>();
        }
    
        public virtual int ID { get; set; }
        public virtual string NOME { get; set; }
        public virtual string FK_PRICELIST { get; set; }
        public virtual string FK_CULTURE { get; set; }
        public virtual Nullable<int> FK_TEXTTYPE { get; set; }
        public virtual string FK_NAZIONE { get; set; }
        public virtual bool ATTIVA { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual TextType TEXTTYPE { get; set; }
        public virtual ICollection<TextRevision> REVISIONS { get; set; }
    }
}
