using System;
using System.Collections.Generic;

namespace TrinityText.Domain
{
    public partial class Folder : IEntity
    {
        public Folder()
        {
            //this.SUBFOLDERS = new HashSet<Folder>();
            //this.FILES = new HashSet<File>();
        }
    
        public virtual int ID { get; set; }
        public virtual string NAME { get; set; }
        public virtual int? FK_PARENT { get; set; }
        public virtual string NOTE { get; set; }
        public virtual string FK_WEBSITE { get; set; }
        public virtual bool DELETABLE { get; set; }

        //public virtual ICollection<Folder> SUBFOLDERS { get; set; }
        //public virtual Folder PARENT { get; set; }
        //public virtual ICollection<File> FILES { get; set; }
    }
}
