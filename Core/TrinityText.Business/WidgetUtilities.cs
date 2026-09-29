using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public class WidgetUtilities : IWidgetUtilities
    {
        private readonly IFileManagerService _fileManagerService;
        private readonly IWidgetService _widgetService;

        private const int MaxExpandedLength = 5 * 1024 * 1024;

        private const int MaxNestedPasses = 10;

        // The key length is bounded: with an open-ended ".+?" every "@[WIDGET(" without a closing ")]" made the engine
        // scan to the end of the line, so a large input of repeated openers cost quadratic time. The timeout is a
        // second line of defense.
        private static readonly Regex WidgetRegex = new(@"@\[WIDGET\((.{1,255}?)\)\]", RegexOptions.Compiled, TimeSpan.FromSeconds(10));

        // The link pattern depends on the website name (low cardinality), so compiled instances are cached.
        private static readonly ConcurrentDictionary<string, Regex> LinkRegexCache = new(StringComparer.Ordinal);

        private static Regex GetLinkRegex(string website)
            => LinkRegexCache.GetOrAdd(website, w => new Regex(@"""?(@/" + Regex.Escape(w) + @"/)([^""\s\t\]]+)""?", RegexOptions.Compiled, TimeSpan.FromSeconds(10)));

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

            // ordinal comparison: the tokens are ASCII, culture-aware matching only made every pass slower
            return text
                .Replace("@[TENANT]", tenant, StringComparison.OrdinalIgnoreCase)
                .Replace("@[WEBSITE]", website, StringComparison.OrdinalIgnoreCase)
                .Replace("@[PRICELIST]", site, StringComparison.OrdinalIgnoreCase)
                .Replace("@[PARTNER]", tenant, StringComparison.OrdinalIgnoreCase)
                .Replace("@[CHANNEL]", website, StringComparison.OrdinalIgnoreCase)
                .Replace("@[SITE]", site, StringComparison.OrdinalIgnoreCase)
                .Replace("@[LANG]", language, StringComparison.OrdinalIgnoreCase)
                .Replace("@[DATE]", DateTime.Now.ToShortDateString(), StringComparison.OrdinalIgnoreCase);
        }

        public Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language)
            => ReplaceWidget(text, site, website, tenant, language, new WidgetResolutionCache());

        public async Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language, WidgetResolutionCache cache)
        {
            cache ??= new WidgetResolutionCache();

            if (text != null && text.Length > MaxExpandedLength)
            {
                throw new InvalidOperationException($"The content exceeds {MaxExpandedLength} characters");
            }

            var budget = new ExpansionBudget(MaxExpandedLength);
            var expanded = await ExpandWidgets(text, 0, site, website, language, cache, budget);

            return ReplacePlaceholder(expanded, tenant, website, site, language);
        }

        // Nested widgets are expanded recursively (up to MaxNestedPasses levels; deeper tokens stay as they are).
        // The "]]>" protection is applied ONCE to the fully expanded value of each top-level widget: escaping every
        // fragment on its own could be defeated by a widget that ends with "]" nesting one that starts with "]>".
        private async Task<string> ExpandWidgets(string text, int depth, string site, string website, string language, WidgetResolutionCache cache, ExpansionBudget budget)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("@[WIDGET(", StringComparison.Ordinal) < 0)
            {
                budget.Add(text?.Length ?? 0);
                return text;
            }

            var result = new StringBuilder();
            var last = 0;
            foreach (Match match in WidgetRegex.Matches(text))
            {
                var literal = text.Substring(last, match.Index - last);
                budget.Add(literal.Length);
                result.Append(literal);

                if (depth >= MaxNestedPasses)
                {
                    budget.Add(match.Length);
                    result.Append(match.Value);
                }
                else
                {
                    var content = await ResolveWidget(match.Groups[1].Value, site, website, language, cache);
                    var expanded = await ExpandWidgets(content, depth + 1, site, website, language, cache, budget);
                    result.Append(depth == 0 ? CdataSafe(expanded) : expanded);
                }

                last = match.Index + match.Length;
            }

            var tail = text.Substring(last);
            budget.Add(tail.Length);
            result.Append(tail);

            return result.ToString();
        }

        // raw (not escaped) widget content; the same widget key is requested by every page of an export: resolved once per cache
        private async Task<string> ResolveWidget(string key, string site, string website, string language, WidgetResolutionCache cache)
        {
            var cacheKey = string.Join('\u0001', key, website, site, language);
            if (cache.Widgets.TryGetValue(cacheKey, out var content))
            {
                return content;
            }

            var widgetRs = await _widgetService.GetByKeys(key, website, site, language);
            if (widgetRs.Success)
            {
                content = widgetRs.Value?.Content ?? string.Empty;
            }
            else if (widgetRs.Errors.Any(e => e.Description == "NOT_FOUND"))
            {
                // unknown key: the placeholder text stays visible (existing behavior)
                content = key;
            }
            else
            {
                // a lookup error (e.g. database down) must not be cached and published as if the widget were its own key
                throw new InvalidOperationException($"Unable to resolve widget '{key}'");
            }

            cache.Widgets[cacheKey] = content;
            return content;
        }

        private sealed class ExpansionBudget(int limit)
        {
            private long _used;

            public void Add(int length)
            {
                _used += length;
                if (_used > limit)
                {
                    throw new InvalidOperationException($"Widget expansion exceeds {limit} characters");
                }
            }
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
