using System.Collections.Generic;

namespace TrinityText.Domain
{
    public partial class CdnServer
    {
        public CdnServer()
        {
            this.CDNSERVERPERWEBSITES = new HashSet<CdnServersPerWebsite>();
            this.PUBLICATIONS = new HashSet<Publication>();
            this.FTPSERVERS = new HashSet<FtpServerPerCdnServer>();
            this.CACHESETTINGS = new HashSet<CacheSettings>();
        }
    
        public virtual int ID { get; set; }
        public virtual string NAME { get; set; }
        public virtual string BASEURL { get; set; }
        public virtual int TYPE { get; set; }
    
        public virtual ICollection<CdnServersPerWebsite> CDNSERVERPERWEBSITES { get; set; }
        public virtual ICollection<Publication> PUBLICATIONS { get; set; }
        public virtual ICollection<FtpServerPerCdnServer> FTPSERVERS { get; set; }
        public virtual ICollection<CacheSettings> CACHESETTINGS { get; set; }
    }
}
