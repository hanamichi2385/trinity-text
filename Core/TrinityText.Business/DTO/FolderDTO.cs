using System.Collections.Generic;

namespace TrinityText.Business
{
    public class FolderDTO
    {
        public FolderDTO()
        {
            this.SubFolders = new List<FolderDTO>();
        }

        public int? Id { get; set; }

        public string Name { get; set; }

        //public string ClientId { get; set; }

        public FolderDTO ParentFolder { get; set; }

        public IList<FolderDTO> SubFolders { get; set; }

        public bool Deletable { get; set; }

        public string Note { get; set; }

        public string Website { get; set; }
    }
}