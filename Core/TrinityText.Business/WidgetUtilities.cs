using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public class WidgetUtilities : IWidgetUtilities
    {
        private readonly IFileManagerService _fileManagerService;
        private readonly IWidgetService _widgetService;

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
            var newText = text
                .Replace("@[TENANT]", tenant, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[WEBSITE]", website, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[PRICELIST]", site, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[PARTNER]", tenant, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[CHANNEL]", website, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[SITE]", site, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[LANG]", language, StringComparison.InvariantCultureIgnoreCase)
                .Replace("@[DATE]", DateTime.Now.ToShortDateString(), StringComparison.InvariantCultureIgnoreCase);

            return newText;
        }

        public async Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language)
        {
            var newText = text;

            var pattern = @$"(\@\[" + Regex.Escape("WIDGET") + @"\()(.+?)(\)\])";
            var reg = new Regex(pattern);

            while (newText.Contains("@[WIDGET("))
            {
                var matches = reg.Matches(newText)
                    .OfType<Match>()
                    .Select(m => m.Value)
                    .Distinct()
                    .ToArray();

                foreach (var m in matches)
                {
                    var key = m.Substring(9).Replace(")]", string.Empty);

                    var widgetRs = await _widgetService.GetByKeys(key, website, site, language);

                    if (widgetRs.Success)
                    {
                        var widget = widgetRs.Value;
                        var wContenuto = widget?.Content ?? string.Empty;
                        //if (!string.IsNullOrWhiteSpace(widget.Content))
                        //{
                        //    wContenuto = ReplacePlaceholder(tenant, website, site, language, widget.Content);
                        //}
                        newText = newText.Replace($"@[WIDGET({key})]", wContenuto);
                    }
                    else
                    {
                        newText = newText.Replace($"@[WIDGET({key})]", key);
                    }
                }
            }

            return ReplacePlaceholder(newText, tenant, website, site, language);
        }

        public async Task<string> ReplaceLink(string xml, string tenant, string website, string baseUrl, CdnServerDTO cdnServer)
        {
            var newXml = xml;
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                var pattern = @"(""?)(\@\/" + Regex.Escape(website) + @"\/)([^\""\s\t\]]+)(""?)";

                var reg = new Regex(pattern);
                var matches = reg.Matches(xml)
                    .OfType<Match>()
                    .Select(v => v.Value)
                    .Distinct()
                    .ToArray();

                foreach (var m in matches)
                {
                    var url = m.ToString().Replace("\"", string.Empty);

                    var fileRs = await _fileManagerService.GetFileByFullname(url);

                    if (fileRs.Success)
                    {
                        var file = fileRs.Value;
                        var proxy = $"{baseUrl}/Renderize.ashx?id={file.Id}";
                        newXml = newXml.Replace(url, proxy);
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Impossibile risolvere il link \"{url}\". Inserire il file mancante sul File Manager e/o verificare che sia nel percorso indicato.");
                    }
                }
            }

            var vendorName = website;
            var oldPath = $"@/{website}";

            var newPath = $"/Media/{tenant}/{website}";
            if (cdnServer != null && !string.IsNullOrWhiteSpace(cdnServer.BaseUrl))
            {
                newPath = $"{cdnServer.BaseUrl}/Media/{tenant}/{website}";
            }

            newXml = newXml.Replace(oldPath, newPath);
            return newXml;
        }
    }
}
