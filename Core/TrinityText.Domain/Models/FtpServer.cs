using System;
using System.Collections.Generic;

namespace TrinityText.Domain
{
    public partial class FtpServer : IEntity
    {
        public FtpServer()
        {
            this.PUBLICATIONS = new HashSet<Publication>();
            this.CDNSERVERS = new HashSet<FtpServerPerCdnServer>();
        }
    
        public virtual int ID { get; set; }
        public virtual string NAME { get; set; }
        public virtual string HOST { get; set; }
        public virtual Nullable<int> PORT { get; set; }
        public virtual string USERNAME { get; set; }
        public virtual string PASSWORD { get; set; }
        public virtual int TYPE { get; set; }

        public virtual ICollection<Publication> PUBLICATIONS { get; set; }
        public virtual ICollection<FtpServerPerCdnServer> CDNSERVERS { get; set; }
    }
}
