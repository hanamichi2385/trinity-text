using System.Collections.Generic;

namespace TrinityText.Business
{
    public class PageTypeDTO
    {
        public int? Id { get; set; }

        public string Name { get; set; }

        public string Schema { get; set; }

        public string Website { get; set; }

        public string Subfolder { get; set; }

        public string OutputFilename { get; set; }

        public string PathPreviewPage { get; set; }

        public int PageTotals { get; set; }

        public string PrintElementName { get; set; }

        public IList<string> Visibility { get; set; }

        public bool HasSubfolder
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.Subfolder);
            }
        }
    }
}