using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace TrinityText.Business
{
    /// <summary>
    /// XML parsing with DTD processing prohibited (no entity expansion / XXE surface) and bounded size and depth
    /// (the JSON conversion of a page is recursive: a document nested thousands of levels deep would overflow the stack
    /// of the worker, which cannot be caught). Page schemas and contents never need a DTD.
    /// </summary>
    internal static class SafeXml
    {
        public const int MaxDepth = 200;

        public const long MaxCharacters = 20_000_000;

        private static readonly XmlReaderSettings Settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = true, // same as XDocument.Parse/Load with LoadOptions.None
            MaxCharactersInDocument = MaxCharacters,
        };

        public static XDocument ParseDocument(string xml)
        {
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, Settings);
            return EnsureDepth(XDocument.Load(reader));
        }

        public static XDocument LoadDocument(Stream stream)
        {
            using var reader = XmlReader.Create(stream, Settings);
            return EnsureDepth(XDocument.Load(reader));
        }

        public static XElement ParseElement(string xml)
            => ParseDocument(xml).Root;

        private static XDocument EnsureDepth(XDocument document)
        {
            if (document.Root == null)
            {
                return document;
            }

            // iterative: a recursive check would overflow on the very documents it has to reject
            var stack = new Stack<(XElement Element, int Depth)>();
            stack.Push((document.Root, 1));
            while (stack.Count > 0)
            {
                var (element, depth) = stack.Pop();
                if (depth > MaxDepth)
                {
                    throw new XmlException($"The XML is nested more than {MaxDepth} levels");
                }

                foreach (var child in element.Elements())
                {
                    stack.Push((child, depth + 1));
                }
            }

            return document;
        }
    }
}
