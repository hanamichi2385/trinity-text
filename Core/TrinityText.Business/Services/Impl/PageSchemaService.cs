using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TrinityText.Business.Schema;

namespace TrinityText.Business.Services.Impl
{
    public class PageSchemaService : IPageSchemaService
    {
        private readonly IWidgetUtilities _widgetUtilities;

        public PageSchemaService(IWidgetUtilities widgetUtilities)
        {
            _widgetUtilities = widgetUtilities;
        }

        public PageSchema GetContentStructure(string xml)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(xml);
            using var stream = new MemoryStream(byteArray);
            PageSchema structure = GetContentStructure(stream);
            stream.Close();

            return structure;
        }

        public PageSchema GetContentStructure(Stream stream)
        {
            var doc = XDocument.Load(stream);
            var root = doc.Root;

            var id = root.Attribute("id");


            var content = root.Element("content");
            var contentId = content.Attribute("id");
            PageSchema pageSchema = new PageSchema() { RootName = id.Value, ChildName = contentId.Value };

            foreach (var part in content.Elements())
            {
                string partName = part.Name.ToString().ToLower();
                bool isRequired = part.Attribute("isrequired") != null && bool.Parse(part.Attribute("isrequired").Value);
                string partId = part.Attribute("id") != null ? part.Attribute("id").Value : string.Empty;
                string description = part.Attribute("description") != null ? part.Attribute("description").Value : string.Empty;

                switch (partName)
                {
                    case "textatom":
                    case "textpart":
                        var textAtom = new TextAtom();
                        bool isHtml = part.Attribute("ishtml") != null && bool.Parse(part.Attribute("ishtml").Value);
                        int? maxLenght = part.Attribute("maxlenght") != null ? int.Parse(part.Attribute("maxlenght").Value) : int.MaxValue;
                        int? minLenght = part.Attribute("minlenght") != null ? int.Parse(part.Attribute("minlenght").Value) : int.MinValue;
                        textAtom.Id = partId;
                        textAtom.IsHtml = isHtml;
                        textAtom.IsRequired = isRequired;
                        textAtom.MaxValue = maxLenght;
                        textAtom.MinValue = minLenght;
                        textAtom.Description = description;

                        pageSchema.Body.Add(textAtom);
                        break;

                    case "imageatom":
                    case "imagepart":
                        var imageAtom = new ImageAtom
                        {
                            IsRequired = isRequired,
                            Id = partId,
                            Description = description
                        };
                        pageSchema.Body.Add(imageAtom);
                        break;

                    case "galleryatom":
                    case "gallerypart":
                        var galleryAtom = new GalleryAtom
                        {
                            IsRequired = isRequired,
                            Id = partId,
                            Description = description
                        };
                        var item = part.Elements().FirstOrDefault();

                        if (item != null)
                        {
                            var name = item.Attribute("name");
                            galleryAtom.ItemName = name.Value;
                        }
                        pageSchema.Body.Add(galleryAtom);
                        break;

                    //case "blogpart":
                    //    textAtom.Type = TrinityText.Business.Schema.AtomType.Blog;
                    //    textAtom.IsRequired = isRequired;

                    //    var news = part.Elements().FirstOrDefault();

                    //    if (news != null)
                    //    {
                    //        var name = news.Attribute("name");
                    //        textAtom.ItemName = name.Value;
                    //    }

                    //    break;

                    case "numberatom":
                    case "numberpart":
                        var numberAtom = new NumberAtom();
                        int? minValue = null;
                        if (part.Attribute("minvalue") != null)
                        {
                            minValue = int.Parse(part.Attribute("minvalue").Value);
                        }
                        int? maxValue = null;
                        if (part.Attribute("maxvalue") != null)
                        {
                            maxValue = int.Parse(part.Attribute("maxvalue").Value);
                        }
                        numberAtom.IsRequired = isRequired;
                        numberAtom.MinValue = minValue;
                        numberAtom.MaxValue = maxValue;
                        numberAtom.Id = partId;
                        numberAtom.Description = description;
                        pageSchema.Body.Add(numberAtom);
                        break;

                    case "checkboxatom":
                    case "checkboxpart":
                        var checkboxAtom = new CheckBoxAtom
                        {
                            IsRequired = isRequired,
                            Id = partId,
                            Description = description
                        };
                        pageSchema.Body.Add(checkboxAtom);
                        break;

                    default:
                        break;
                }
            }
            return pageSchema;
        }

        public string GetXmlFromContent(PageSchema pageSchema)
        {
            var doc = new XDocument();
            //XElement root = new XElement(PageSchema.RootName);

            var content = new XElement(pageSchema.ChildName);

            foreach (var part in pageSchema.Body)
            {
                var elementPart = new XElement(part.Id);
                switch (part.Type)
                {
                    case TrinityText.Business.Schema.AtomType.Text:
                        var text = part as TextAtom;
                        var dataText = new XCData(string.IsNullOrEmpty(text.Value) ? string.Empty : text.Value);
                        elementPart.Add(dataText);
                        break;
                    case TrinityText.Business.Schema.AtomType.Number:
                        var number = part as NumberAtom;
                        var dataNumber = new XCData(string.IsNullOrEmpty(number.Value) ? string.Empty : number.Value);
                        elementPart.Add(dataNumber);
                        break;
                    case TrinityText.Business.Schema.AtomType.Image:
                        var image = part as ImageAtom;
                        var imageUrl = new XElement("url");
                        var imageUrlData = new XCData(string.IsNullOrEmpty(image.Value) ? string.Empty : image.Value);
                        imageUrl.Add(imageUrlData);
                        elementPart.Add(imageUrl);

                        var imageCaption = new XElement("caption");
                        var imageCaptionData = new XCData(string.IsNullOrEmpty(image.Caption) ? string.Empty : image.Caption);
                        imageCaption.Add(imageCaptionData);
                        elementPart.Add(imageCaption);

                        var imageLink = new XElement("link");
                        var imageLinkData = new XCData(string.IsNullOrEmpty(image.Link) ? string.Empty : image.Link);
                        imageLink.Add(imageLinkData);
                        elementPart.Add(imageLink);

                        break;
                    case TrinityText.Business.Schema.AtomType.Gallery:
                        var gallery = part as GalleryAtom;
                        foreach (var i in gallery.Items)
                        {
                            if (!i.IsEmpty)
                            {
                                var item = new XElement(gallery.ItemName);

                                var path = new XElement("path");
                                var pathData = new XCData(string.IsNullOrWhiteSpace(i.Path) ? string.Empty : i.Path);
                                path.Add(pathData);

                                var caption = new XElement("caption");
                                var captionData = new XCData(string.IsNullOrWhiteSpace(i.Caption) ? string.Empty : i.Caption);
                                caption.Add(captionData);

                                var link = new XElement("link");
                                var linkData = new XCData(string.IsNullOrWhiteSpace(i.Link) ? string.Empty : i.Link);
                                link.Add(linkData);

                                var order = new XElement("order")
                                {
                                    Value = i.Order.HasValue ? i.Order.Value.ToString() : "0"
                                };
                                //XCData orderData = new XCData(string.IsNullOrWhiteSpace(i.Order) ? string.Empty : i.Order);
                                //order.Add(orderData);

                                item.Add(path);
                                item.Add(caption);
                                item.Add(link);
                                item.Add(order);

                                elementPart.Add(item);
                            }
                        }
                        break;

                    //case TrinityText.Business.Schema.AtomType.Blog:
                    //    foreach (var n in part.News)
                    //    {
                    //        if (!n.IsEmpty)
                    //        {
                    //            var news = new XElement(part.ItemName);

                    //            var title = new XElement("title");
                    //            XCData titleData = new XCData(string.IsNullOrEmpty(n.Title) ? string.Empty : n.Title);
                    //            title.Add(titleData);

                    //            var author = new XElement("author");
                    //            XCData authorData = new XCData(string.IsNullOrEmpty(n.Author) ? string.Empty : n.Author);
                    //            author.Add(authorData);

                    //            var link = new XElement("link");
                    //            XCData linkData = new XCData(string.IsNullOrEmpty(n.Link) ? string.Empty : n.Link);
                    //            link.Add(linkData);

                    //            var text = new XElement("text");
                    //            XCData textData = new XCData(string.IsNullOrEmpty(n.Text) ? string.Empty : n.Text);
                    //            text.Add(textData);

                    //            news.Add(title);
                    //            news.Add(text);
                    //            news.Add(author);
                    //            news.Add(link);

                    //            elementPart.Add(news);
                    //        }
                    //    }
                    //    break;
                    case TrinityText.Business.Schema.AtomType.Checkbox:
                        var checkbox = part as CheckBoxAtom;
                        elementPart.Add(string.IsNullOrWhiteSpace(checkbox.Value) ? "false" : checkbox.Value.ToLower());
                        break;
                }
                content.Add(elementPart);
            }

            doc.Document.Add(content);
            return doc.ToString(SaveOptions.DisableFormatting);
        }

        public PageSchema ParseContent(Stream stream, PageSchema structure)
        {
            var doc = XDocument.Load(stream);
            var root = doc.Root;

            var rootPart = new PageSchema() { RootName = structure.RootName, ChildName = root.Name.LocalName };

            foreach (var part in structure.Body)
            {
                var element = root.Elements().
                    Where(p => p.Name.LocalName.Equals(part.Id))
                    .SingleOrDefault();

                var clonePart = part.Clone();
                if (element != null)
                {
                    switch (part.Type)
                    {
                        case TrinityText.Business.Schema.AtomType.Text:
                            var text = clonePart as TextAtom;
                            text.Value = element.Value;
                            rootPart.Body.Add(text);
                            break;
                        case TrinityText.Business.Schema.AtomType.Number:
                            var number = clonePart as NumberAtom;
                            number.Value = element.Value;
                            rootPart.Body.Add(number);
                            break;
                        case TrinityText.Business.Schema.AtomType.Checkbox:
                            var checkbox = clonePart as CheckBoxAtom;

                            if (string.IsNullOrWhiteSpace(element.Value))
                            {
                                checkbox.Value = "false";
                            }
                            else
                            {
                                checkbox.Value = element.Value;
                            }

                            rootPart.Body.Add(checkbox);
                            break;
                        case TrinityText.Business.Schema.AtomType.Image:
                            var imageAtom = clonePart as ImageAtom;
                            imageAtom.Value = element.Element("url") != null ? element.Element("url").Value : string.Empty;
                            imageAtom.Caption = element.Element("caption") != null ? element.Element("caption").Value : string.Empty;
                            imageAtom.Link = element.Element("link") != null ? element.Element("link").Value : string.Empty;
                            rootPart.Body.Add(imageAtom);
                            break;
                        case TrinityText.Business.Schema.AtomType.Gallery:
                            var gallery = clonePart as GalleryAtom;
                            foreach (var item in element.Elements(gallery.ItemName))
                            {
                                var path = item.Element("path") != null ? item.Element("path").Value : string.Empty;
                                var caption = item.Element("caption") != null ? item.Element("caption").Value : string.Empty;
                                var link = item.Element("link") != null ? item.Element("link").Value : string.Empty;
                                var order = 0;

                                var orderValue = item.Element("order") != null ? item.Element("order").Value : string.Empty;
                                if (!string.IsNullOrWhiteSpace(orderValue))
                                {
                                    if(int.TryParse(orderValue, out order) == false)
                                    {
                                        order = 0;
                                    }
                                }

                                var itemPart = new ImageParticol()
                                {
                                    Path = path,
                                    Caption = caption,
                                    Link = link,
                                    Order = order,
                                };
                                gallery.Items.Add(itemPart);
                            }
                            rootPart.Body.Add(gallery);
                            break;

                            //case TrinityText.Business.Schema.AtomType.Blog:
                            //    foreach (var item in element.Elements(clonePart.ItemName))
                            //    {
                            //        var title = item.Element("title") != null ? item.Element("title").Value : string.Empty;
                            //        var text = item.Element("text") != null ? item.Element("text").Value : string.Empty;
                            //        var author = item.Element("author") != null ? item.Element("author").Value : string.Empty;
                            //        var link = item.Element("link") != null ? item.Element("link").Value : string.Empty;

                            //        NewsPart newsPart = new NewsPart()
                            //        {
                            //            Title = title,
                            //            Text = text,
                            //            Link = link,
                            //            Author = author,
                            //        };
                            //        clonePart.News.Add(newsPart);
                            //    }
                            //    break;
                    }
                }
                else
                {
                    switch (part.Type)
                    {
                        case TrinityText.Business.Schema.AtomType.Text:
                            var text = clonePart as TextAtom;
                            rootPart.Body.Add(text);
                            break;
                        case TrinityText.Business.Schema.AtomType.Number:
                            var number = clonePart as NumberAtom;
                            rootPart.Body.Add(number);
                            break;
                        case TrinityText.Business.Schema.AtomType.Checkbox:
                            var checkbox = clonePart as CheckBoxAtom;
                            rootPart.Body.Add(checkbox);
                            break;
                        case TrinityText.Business.Schema.AtomType.Image:
                            var imageAtom = clonePart as ImageAtom;
                            rootPart.Body.Add(imageAtom);
                            break;
                        case TrinityText.Business.Schema.AtomType.Gallery:
                            var gallery = clonePart as GalleryAtom;
                            rootPart.Body.Add(gallery);
                            break;

                            //case TrinityText.Business.Schema.AtomType.Blog:
                            //    foreach (var item in element.Elements(clonePart.ItemName))
                            //    {
                            //        var title = item.Element("title") != null ? item.Element("title").Value : string.Empty;
                            //        var text = item.Element("text") != null ? item.Element("text").Value : string.Empty;
                            //        var author = item.Element("author") != null ? item.Element("author").Value : string.Empty;
                            //        var link = item.Element("link") != null ? item.Element("link").Value : string.Empty;

                            //        NewsPart newsPart = new NewsPart()
                            //        {
                            //            Title = title,
                            //            Text = text,
                            //            Link = link,
                            //            Author = author,
                            //        };
                            //        clonePart.News.Add(newsPart);
                            //    }
                            //    break;
                    }
                }


            }
            return rootPart;
        }

        public PageSchema ParseContent(string xml, PageSchema structure)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return structure;
            }
            else
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(xml);
                using var stream = new MemoryStream(byteArray);
                var pageSchema = ParseContent(stream, structure);
                stream.Close();

                return pageSchema;
            }
        }

        public async Task<byte[]> CreateXmlContentsDocument(PageSchema structure, IList<PageDTO> contentsPerType, string tenant, string vendor, string instance, string language, string baseUrl, CdnServerDTO cdnServer)
        {
            var doc = new XDocument();
            var declaration = new XDeclaration("1.0", "utf-8", string.Empty);
            doc.Declaration = declaration;
            var root = new XElement(structure.RootName);

            foreach (var c in contentsPerType)
            {
                var xml = await _widgetUtilities.Replace(tenant, vendor, instance, language, c.Content);

                xml = await _widgetUtilities.ReplaceLink(xml, tenant, vendor, baseUrl, cdnServer);

                XElement element = XElement.Parse(xml);
                root.Add(element);
            }
            doc.Add(root);
            var file = doc.ToString();

            return Encoding.UTF8.GetBytes(file);
        }

        public async Task<byte[]> CreateJsonContentsDocument(PageSchema structure, IList<PageDTO> contentsPerType, string tenant, string vendor, string instance, string language, string baseUrl, CdnServerDTO cdnServer)
        {
            var list = new List<dynamic>();

            foreach (var c in contentsPerType)
            {
                var xml = await _widgetUtilities.Replace(tenant, vendor, instance, language, c.Content);

                xml = await _widgetUtilities.ReplaceLink(xml, tenant, vendor, baseUrl, cdnServer);

                var element = XElement.Parse(xml);
                var node_cdata = element.DescendantNodes().OfType<XCData>().ToList();

                foreach (var node in node_cdata)
                {
                    node.Parent.Add(node.Value);
                    node.Remove();
                }

                var jsontext = JsonConvert.SerializeXNode(element, Newtonsoft.Json.Formatting.None, false);
                list.Add(jsontext);
            }
            var file = JsonConvert.SerializeObject(list);
            return Encoding.UTF8.GetBytes(file);
        }
    }
}
