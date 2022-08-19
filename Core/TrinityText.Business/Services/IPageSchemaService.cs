using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TrinityText.Business.Schema;

namespace TrinityText.Business
{
    public interface IPageSchemaService
    {
        Task<byte[]> CreateJsonContentsDocument(PageSchema structure, IList<PageDTO> contentsPerType, string tenant, string website, string site, string language, string baseUrl, CdnServerDTO cdnServer);
        Task<byte[]> CreateXmlContentsDocument(PageSchema structure, IList<PageDTO> contentsPerType, string tenant, string website, string site, string language, string baseUrl, CdnServerDTO cdnServer);
        PageSchema GetContentStructure(Stream stream);
        PageSchema GetContentStructure(string xml);
        string GetXmlFromContent(PageSchema pageSchema);
        PageSchema ParseContent(Stream stream, PageSchema structure);
        PageSchema ParseContent(string xml, PageSchema structure);
    }
}
