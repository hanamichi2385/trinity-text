using System;
using System.Collections.Generic;

namespace TrinityText.Domain
{
    public partial class FtpServer
    {
        public FtpServer()
        {
            this.PUBLICATIONS = new HashSet<Publication>();
        }
    
        public virtual int ID { get; set; }
        public virtual string NAME { get; set; }
        public virtual string HOST { get; set; }
        public virtual Nullable<int> PORT { get; set; }
        public virtual string USERNAME { get; set; }
        public virtual string PASSWORD { get; set; }
        public virtual int TYPE { get; set; }

        public virtual int FK_CDNSERVER { get; set; }
    
        public virtual ICollection<Publication> PUBLICATIONS { get; set; }
        public virtual CdnServer CDNSERVER { get; set; }
    }
}
