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
            var newText = text;

            newText = newText.Replace("@[PARTNER]", tenant, StringComparison.InvariantCultureIgnoreCase);
            newText = newText.Replace("@[CHANNEL]", website, StringComparison.InvariantCultureIgnoreCase);
            newText = newText.Replace("@[SITE]", site, StringComparison.InvariantCultureIgnoreCase);
            newText = newText.Replace("@[LANG]", language, StringComparison.InvariantCultureIgnoreCase);
            newText = newText.Replace("@[DATE]", DateTime.Now.ToShortDateString(), StringComparison.InvariantCultureIgnoreCase);
            newText = await ReplaceWidget(newText, site, website, tenant, language);

            return newText;
        }

        public async Task<string> ReplaceWidget(string text, string site, string website, string tenant, string language)
        {
            var newText = text;

            var pattern = @"(\@\[" + Regex.Escape("WIDGET") + @"\()(.+?)(\)\])";
            Regex reg = new Regex(pattern);

            while (newText.Contains("@[WIDGET("))
            {
                var matches = reg.Matches(newText)
                    .OfType<Match>()
                    .Select(m => m.Value)
                    .Distinct()
                    .ToList();

                foreach (var m in matches)
                {
                    var chiave = m.Substring(9).Replace(")]", string.Empty);

                    var widgetRs = await _widgetService.GetByKeys(chiave, website, site, language);

                    if (widgetRs.Success)
                    {
                        var widget = widgetRs.Value;
                        var wContenuto = string.Empty;
                        if (!string.IsNullOrEmpty(widget.Content))
                        {
                            wContenuto = widget.Content;
                            wContenuto = wContenuto.Replace("@[PARTNER]", tenant, StringComparison.InvariantCultureIgnoreCase);
                            wContenuto = wContenuto.Replace("@[CHANNEL]", website, StringComparison.InvariantCultureIgnoreCase);
                            wContenuto = wContenuto.Replace("@[SITE]", site, StringComparison.InvariantCultureIgnoreCase);
                            wContenuto = wContenuto.Replace("@[LANG]", language, StringComparison.InvariantCultureIgnoreCase);
                            wContenuto = wContenuto.Replace("@[DATE]", DateTime.Now.ToShortDateString(), StringComparison.InvariantCultureIgnoreCase);
                        }
                        newText = newText.Replace(string.Format("@[WIDGET({0})]", chiave), wContenuto);
                    }
                    else
                    {
                        newText = newText.Replace(string.Format("@[WIDGET({0})]", chiave), string.Format("", chiave));
                    }
                }
            }

            return newText;
        }

        public async Task<string> ReplaceLink(string xml, string tenant, string website, string baseUrl, CdnServerDTO cdnServer)
        {
            var newXml = xml;
            if (!string.IsNullOrEmpty(baseUrl))
            {
                var pattern = @"(""?)(\@\/" + Regex.Escape(website) + @"\/)([^\""\s\t\]]+)(""?)";

                Regex reg = new Regex(pattern);
                var matches = reg.Matches(xml)
                    .OfType<Match>()
                    .Select(v => v.Value)
                    .Distinct()
                    .ToList();

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
                        throw new KeyNotFoundException(string.Format("Impossibile risolvere il link \"{0}\". Inserire il file mancante sul File Manager e/o verificare che sia nel percorso indicato.", url));
                    }
                }
            }

            var vendorName = website;
            var oldPath = string.Format("@/{0}", website);

            var newPath = string.Format("/Media/{0}/{1}", tenant, website);
            if (cdnServer != null && !string.IsNullOrEmpty(cdnServer.BaseUrl))
            {
                newPath = string.Format("{0}/Media/{1}/{2}", cdnServer.BaseUrl, tenant, website);
            }

            newXml = newXml.Replace(oldPath, newPath);
            return newXml;
        }
    }
}
