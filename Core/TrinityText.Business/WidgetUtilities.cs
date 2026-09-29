using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public class WidgetUtilities : IWidgetUtilities
    {
        private readonly IFileManagerService _fileManagerService;
        private readonly IWidgetService _widgetService;

        private const int MaxExpandedLength = 5 * 1024 * 1024;

        private static readonly Regex WidgetRegex = new(@"@\[WIDGET\((.+?)\)\]", RegexOptions.Compiled);

        // The link pattern depends on the website name (low cardinality), so compiled instances are cached.
        private static readonly ConcurrentDictionary<string, Regex> LinkRegexCache = new(StringComparer.Ordinal);

        private static Regex GetLinkRegex(string website)
            => LinkRegexCache.GetOrAdd(website, w => new Regex(@"""?(@/" + Regex.Escape(w) + @"/)([^""\s\t\]]+)""?", RegexOptions.Compiled));

        public WidgetUtilities(IFileManagerService fileManagerService, IWidgetService widgetService)
        {
            _fileManagerService = fileManagerService;
            _widgetService = widgetService;
        }

        public Task<string> Replace(string tenant, string website, string site, string language, string text)
            => Replace(tenant, website, site, language, text, new WidgetResolutionCache());

        public async Task<string> Replace(string tenant, string website, string site, string language, string text, WidgetResolutionCache cache)
        {
            var replaced = await ReplaceWidget(text, site, website, tenant, language, cache);

            return replaced;
        }

        // Page contents are stored inside CDATA sections: a "]]>" coming from a widget or a placeholder value
        // would close the section early and let arbitrary markup into the published document.
        private static string CdataSafe(string value)
            => string.IsNullOrEmpty(value) ? value : value.Replace("]]>", "]]]]><![CDATA[>", StringComparison.Ordinal);

        private static string ReplacePlaceholder(string text, string tenant, string website, string site, string language)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("@[", StringComparison.Ordinal) < 0)
                return text;

            tenant = CdataSafe(tenant);
            website = CdataSafe(website);
            site = CdataSafe(site);
            language = CdataSafe(language);

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

        public Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language)
            => ReplaceWidget(text, site, website, tenant, language, new WidgetResolutionCache());

        public async Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language, WidgetResolutionCache cache)
        {
            cache ??= new WidgetResolutionCache();
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
                    // the same widget key is requested by every page of an export: resolve it once per cache
                    var cacheKey = string.Join('\u0001', key, website, site, language);
                    if (!cache.Widgets.TryGetValue(cacheKey, out var content))
                    {
                        var widgetRs = await _widgetService.GetByKeys(key, website, site, language);
                        content = widgetRs.Success
                            ? CdataSafe(widgetRs.Value?.Content ?? string.Empty)
                            : key;
                        cache.Widgets[cacheKey] = content;
                    }

                    resolved[key] = content;
                }

                var previous = newText;

                // nested widgets can multiply the output on each pass: bound the total size while expanding
                long expanded = 0;
                newText = WidgetRegex.Replace(newText, m =>
                {
                    var value = resolved[m.Groups[1].Value] ?? string.Empty;
                    expanded += value.Length;
                    if (expanded > MaxExpandedLength)
                    {
                        throw new InvalidOperationException($"Widget expansion exceeds {MaxExpandedLength} characters");
                    }

                    return value;
                });

                if (newText.Length > MaxExpandedLength)
                {
                    throw new InvalidOperationException($"Widget expansion exceeds {MaxExpandedLength} characters");
                }

                if (string.Equals(previous, newText, StringComparison.Ordinal))
                {
                    break;
                }
            }

            return ReplacePlaceholder(newText, tenant, website, site, language);
        }

        public Task<string> ReplaceLink(string xml, string tenant, string website, string baseUrl, CdnServerDTO cdnServer)
            => ReplaceLink(xml, tenant, website, baseUrl, cdnServer, new WidgetResolutionCache());

        public async Task<string> ReplaceLink(string xml, string tenant, string website, string baseUrl, CdnServerDTO cdnServer, WidgetResolutionCache cache)
        {
            cache ??= new WidgetResolutionCache();
            var newXml = xml;
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                var linkRegex = GetLinkRegex(website);

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
                        if (!cache.Links.TryGetValue(url, out var fileId))
                        {
                            // only the id is needed: do not load the file content
                            var fileRs = await _fileManagerService.GetFileIdByFullname(url);
                            if (!fileRs.Success)
                            {
                                throw new KeyNotFoundException($"Impossibile risolvere il link \"{url}\". Inserire il file mancante sul File Manager e/o verificare che sia nel percorso indicato.");
                            }

                            fileId = fileRs.Value;
                            cache.Links[url] = fileId;
                        }
                        resolved[url] = $"{baseUrl}/Renderize.ashx?id={fileId}";
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

    /// <summary>
    /// Memoizes widget and file-link lookups across the pages of a single export
    /// (create one per document/publication, do not keep it across publications).
    /// </summary>
    public sealed class WidgetResolutionCache
    {
        internal Dictionary<string, string> Widgets { get; } = new(StringComparer.Ordinal);

        internal Dictionary<string, Guid> Links { get; } = new(StringComparer.Ordinal);
    }
}
