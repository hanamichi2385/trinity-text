using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TrinityText.Business.Schema;

namespace TrinityText.Business
{
    public interface IPageSchemaService
    {
        /// <param name="cache">Shared by all the documents of an export: each widget / link is looked up once instead of once per document.</param>
        Task<byte[]> CreateJsonContentsDocument(PageSchema structure, IList<PageDTO> contentsPerType, string tenant, string website, string site, string language, string baseUrl, CdnServerDTO cdnServer, WidgetResolutionCache cache = null);
        Task<byte[]> CreateXmlContentsDocument(PageSchema structure, IList<PageDTO> contentsPerType, string tenant, string website, string site, string language, string baseUrl, CdnServerDTO cdnServer, WidgetResolutionCache cache = null);
        PageSchema GetContentStructure(Stream stream);
        PageSchema GetContentStructure(string xml);
        string GetXmlFromContent(PageSchema pageSchema);
        PageSchema ParseContent(Stream stream, PageSchema structure);
        PageSchema ParseContent(string xml, PageSchema structure);
    }
}
