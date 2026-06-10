using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public class WidgetUtilities : IWidgetUtilities
    {
        private readonly IFileManagerService _fileManagerService;
        private readonly IWidgetService _widgetService;

        private static readonly Regex WidgetRegex = new(@"@\[WIDGET\((.+?)\)\]", RegexOptions.Compiled);

        public WidgetUtilities(IFileManagerService fileManagerService, IWidgetService widgetService)
        {
            _fileManagerService = fileManagerService;
            _widgetService = widgetService;
        }

        public async Task<string> Replace(string tenant, string website, string site, string language, string text)
        {
            var replaced = await ReplaceWidget(text, site, website, tenant, language);

            return replaced;
        }

        private static string ReplacePlaceholder(string text, string tenant, string website, string site, string language)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("@[", StringComparison.Ordinal) < 0)
                return text;

            return text
                .Replace("@[TENANT]", tenant, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[WEBSITE]", website, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[PRICELIST]", site, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[PARTNER]", tenant, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[CHANNEL]", website, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[SITE]", site, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[LANG]", language, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[DATE]", DateTime.Now.ToShortDateString(), StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language)
        {
            var newText = text;
            var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
            const int maxNestedPasses = 10;
            var pass = 0;

            while (pass++ < maxNestedPasses && newText.IndexOf("@[WIDGET(", StringComparison.Ordinal) >= 0)
            {
                var matches = WidgetRegex.Matches(newText);
                if (matches.Count == 0)
                {
                    break;
                }

                var newKeys = new List<string>();
                foreach (Match m in matches)
                {
                    var key = m.Groups[1].Value;
                    if (!resolved.ContainsKey(key))
                    {
                        resolved[key] = null;
                        newKeys.Add(key);
                    }
                }

                foreach (var key in newKeys)
                {
                    var widgetRs = await _widgetService.GetByKeys(key, website, site, language);
                    resolved[key] = widgetRs.Success
                        ? (widgetRs.Value?.Content ?? string.Empty)
                        : key;
                }

                var previous = newText;
                newText = WidgetRegex.Replace(newText, m => resolved[m.Groups[1].Value] ?? string.Empty);

                if (string.Equals(previous, newText, StringComparison.Ordinal))
                {
                    break;
                }
            }

            return ReplacePlaceholder(newText, tenant, website, site, language);
        }

        public async Task<string> ReplaceLink(string xml, string tenant, string website, string baseUrl, CdnServerDTO cdnServer)
        {
            var newXml = xml;
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                var linkRegex = new Regex(@"""?(@/" + Regex.Escape(website) + @"/)([^""\s\t\]]+)""?", RegexOptions.Compiled);

                var urlSet = new HashSet<string>(StringComparer.Ordinal);
                foreach (Match m in linkRegex.Matches(xml))
                {
                    urlSet.Add(m.Value.Replace("\"", string.Empty));
                }

                if (urlSet.Count > 0)
                {
                    var resolved = new Dictionary<string, string>(urlSet.Count, StringComparer.Ordinal);
                    foreach (var url in urlSet)
                    {
                        var fileRs = await _fileManagerService.GetFileByFullname(url);
                        if (!fileRs.Success)
                        {
                            throw new KeyNotFoundException($"Impossibile risolvere il link \"{url}\". Inserire il file mancante sul File Manager e/o verificare che sia nel percorso indicato.");
                        }
                        resolved[url] = $"{baseUrl}/Renderize.ashx?id={fileRs.Value.Id}";
                    }

                    foreach (var kv in resolved)
                    {
                        newXml = newXml.Replace(kv.Key, kv.Value);
                    }
                }
            }

            var oldPath = $"@/{website}";
            var newPath = cdnServer != null && !string.IsNullOrWhiteSpace(cdnServer.BaseUrl)
                ? $"{cdnServer.BaseUrl}/Media/{tenant}/{website}"
                : $"/Media/{tenant}/{website}";

            newXml = newXml.Replace(oldPath, newPath);
            return newXml;
        }
    }
}
