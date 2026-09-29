using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IWidgetUtilities
    {
        Task<string> Replace(string tenant, string website, string site, string language, string text);
        Task<string> ReplaceLink(string xml, string tenant, string website, string baseUrl, CdnServerDTO cdnServer);
        Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language);

        /// <summary>Same as the overloads above, sharing widget/link lookups through <paramref name="cache"/> (one per export).</summary>
        Task<string> Replace(string tenant, string website, string site, string language, string text, WidgetResolutionCache cache);
        Task<string> ReplaceLink(string xml, string tenant, string website, string baseUrl, CdnServerDTO cdnServer, WidgetResolutionCache cache);
        Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language, WidgetResolutionCache cache);
    }
}