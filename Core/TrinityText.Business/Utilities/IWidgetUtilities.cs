using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IWidgetUtilities
    {
        Task<string> Replace(string tenant, string website, string site, string language, string text);
        Task<string> ReplaceLink(string xml, string tenant, string website, string baseUrl, CdnServerDTO cdnServer);
        Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language);
    }
}