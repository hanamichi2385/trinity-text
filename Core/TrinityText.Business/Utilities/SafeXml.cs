using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace TrinityText.Business
{
    /// <summary>
    /// XML parsing with DTD processing prohibited (no entity expansion / XXE surface).
    /// Page schemas and contents never need a DTD.
    /// </summary>
    internal static class SafeXml
    {
        private static readonly XmlReaderSettings Settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = true, // same as XDocument.Parse/Load with LoadOptions.None
        };

        public static XDocument ParseDocument(string xml)
        {
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, Settings);
            return XDocument.Load(reader);
        }

        public static XDocument LoadDocument(Stream stream)
        {
            using var reader = XmlReader.Create(stream, Settings);
            return XDocument.Load(reader);
        }

        public static XElement ParseElement(string xml)
            => ParseDocument(xml).Root;
    }
}
