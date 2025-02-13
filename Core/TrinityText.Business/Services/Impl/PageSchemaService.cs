﻿using Newtonsoft.Json;
using System;
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
            var structure = GetContentStructure(stream);
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
            var pageSchema = new PageSchema() { RootName = id.Value, ChildName = contentId.Value };

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
                        bool extend = part.Attribute("extend") != null && bool.Parse(part.Attribute("extend").Value);
                        textAtom.Id = partId;
                        textAtom.IsHtml = isHtml;
                        textAtom.IsRequired = isRequired;
                        textAtom.MaxValue = maxLenght;
                        textAtom.MinValue = minLenght;
                        textAtom.Description = description;
                        textAtom.Extend = extend;

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
                            var captionName = part.Attribute("captionName") != null ? part.Attribute("captionName").Value : string.Empty;
                            galleryAtom.CaptionName = captionName;
                            var captionDescription = part.Attribute("captionDescription") != null ? part.Attribute("captionDescription").Value : string.Empty;
                            galleryAtom.CaptionDescription = captionDescription;
                            var linkName = part.Attribute("linkName") != null ? part.Attribute("linkName").Value : string.Empty;
                            galleryAtom.LinkName = linkName;
                            var linkDescription = part.Attribute("linkDescription") != null ? part.Attribute("linkDescription").Value : string.Empty;
                            galleryAtom.LinkDescription = linkDescription;
                            var pathName = part.Attribute("pathName") != null ? part.Attribute("pathName").Value : string.Empty;
                            galleryAtom.PathName = pathName;
                            var pathDescription = part.Attribute("pathDescription") != null ? part.Attribute("pathDescription").Value : string.Empty;
                            galleryAtom.PathDescription = pathDescription;
                            var useMobileV = false;
                            var useMobile = part.Attribute("useMobile") != null ? bool.TryParse(part.Attribute("useMobile").Value, out useMobileV) : false;
                            galleryAtom.UseMobile = useMobileV || useMobile;
                        }
                        pageSchema.Body.Add(galleryAtom);
                        break;

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

                    case "separatoratom":
                    case "separatorpart":
                        var separatorAtom = new SeparatorAtom
                        {
                            IsRequired = isRequired,
                            Id = partId,
                            Description = description
                        };
                        pageSchema.Body.Add(separatorAtom);
                        break;

                    case "datetimeatom":
                    case "datetimepart":
                        var datetimeAtom = new DateTimeAtom
                        {
                            IsRequired = isRequired,
                            Id = partId,
                            Description = description
                        };
                        pageSchema.Body.Add(datetimeAtom);
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

                        var imageMobile = new XElement("pathMobile");
                        var imageMobileData = new XCData(string.IsNullOrEmpty(image.Mobile) ? string.Empty : image.Mobile);
                        imageMobile.Add(imageMobileData);
                        elementPart.Add(imageMobile);

                        var imageCaption = new XElement("caption");
                        var imageCaptionData = new XCData(string.IsNullOrEmpty(image.Caption) ? string.Empty : image.Caption);
                        imageCaption.Add(imageCaptionData);
                        elementPart.Add(imageCaption);

                        var imageLink = new XElement("link");
                        var imageLinkData = new XCData(string.IsNullOrEmpty(image.Link) ? string.Empty : image.Link);
                        imageLink.Add(imageLinkData);
                        elementPart.Add(imageLink);

                        var imageTarget = new XElement("target");
                        var imageTargetData = new XCData(string.IsNullOrEmpty(image.Target) ? string.Empty : image.Target);
                        imageTarget.Add(imageTargetData);
                        elementPart.Add(imageTarget);

                        var useMobile = new XElement("useMobile")
                        {
                            Value = image.UseMobile.ToString()
                        };
                        elementPart.Add(useMobile);

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

                                var pathmobile = new XElement("pathMobile");
                                var pathmobileData = new XCData(string.IsNullOrWhiteSpace(i.PathMobile) ? string.Empty : i.PathMobile);
                                pathmobile.Add(pathmobileData);

                                var caption = new XElement("caption");
                                var captionData = new XCData(string.IsNullOrWhiteSpace(i.Caption) ? string.Empty : i.Caption);
                                caption.Add(captionData);

                                var link = new XElement("link");
                                var linkData = new XCData(string.IsNullOrWhiteSpace(i.Link) ? string.Empty : i.Link);
                                link.Add(linkData);

                                var target = new XElement("target");
                                var targetData = new XCData(string.IsNullOrWhiteSpace(i.Target) ? string.Empty : i.Target);
                                target.Add(targetData);

                                var order = new XElement("order")
                                {
                                    Value = i.Order.HasValue ? i.Order.Value.ToString() : "0"
                                };
                                //XCData orderData = new XCData(string.IsNullOrWhiteSpace(i.Order) ? string.Empty : i.Order);
                                //order.Add(orderData);

                                item.Add(path);
                                item.Add(pathmobile);
                                item.Add(caption);
                                item.Add(link);
                                item.Add(order);
                                item.Add(target);

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

                    case TrinityText.Business.Schema.AtomType.Separator:
                        var separator = part as SeparatorAtom;
                        break;

                    case TrinityText.Business.Schema.AtomType.DateTime:
                        var dateTime = part as DateTimeAtom;

                        if (DateTime.TryParseExact(dateTime.Value, DateTimeAtom.Format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime date))
                        {
                            elementPart.Add(date.ToString(DateTimeAtom.Format));
                        }
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
                        case TrinityText.Business.Schema.AtomType.Separator:
                            var separator = clonePart as SeparatorAtom;
                            rootPart.Body.Add(separator);
                            break;
                        case TrinityText.Business.Schema.AtomType.DateTime:
                            var dateTime = clonePart as DateTimeAtom;

                            //if (!string.IsNullOrWhiteSpace(element.Value) && DateTime.TryParseExact(element.Value, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime date))
                            //{
                            //    dateTime.Value = date.ToString("dd/MM/yyyy HH:mm:ss");
                            //}
                            dateTime.Value = element.Value;

                            rootPart.Body.Add(dateTime);
                            break;
                        case TrinityText.Business.Schema.AtomType.Image:
                            var imageAtom = clonePart as ImageAtom;
                            imageAtom.Value = element.Element("url") != null ? element.Element("url").Value : string.Empty;
                            imageAtom.Caption = element.Element("caption") != null ? element.Element("caption").Value : string.Empty;
                            imageAtom.Link = element.Element("link") != null ? element.Element("link").Value : string.Empty;
                            imageAtom.Target = element.Element("target") != null ? element.Element("target").Value : string.Empty;
                            imageAtom.Mobile = element.Element("pathMobile") != null ? element.Element("pathMobile").Value : string.Empty;
                            var useMobile = false;
                            if(element.Element("useMobile") != null)
                            {
                                _ = bool.TryParse(element.Element("useMobile").Value, out useMobile);
                            }
                            imageAtom.UseMobile =  useMobile;
                            rootPart.Body.Add(imageAtom);
                            break;
                        case TrinityText.Business.Schema.AtomType.Gallery:
                            var gallery = clonePart as GalleryAtom;
                            foreach (var item in element.Elements(gallery.ItemName))
                            {
                                var path = item.Element("path") != null ? item.Element("path").Value : string.Empty;
                                var pathMobile = item.Element("pathMobile") != null ? item.Element("pathMobile").Value : string.Empty;
                                var target = item.Element("target") != null ? item.Element("target").Value : string.Empty;
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
                                    PathMobile = pathMobile,    
                                    Target = target,
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

                        case TrinityText.Business.Schema.AtomType.DateTime:
                            var dateTime = clonePart as DateTimeAtom;
                            rootPart.Body.Add(dateTime);
                            break;

                        case TrinityText.Business.Schema.AtomType.Separator:
                            var separator = clonePart as SeparatorAtom;
                            rootPart.Body.Add(separator);
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
            var file = doc.ToString(SaveOptions.DisableFormatting);

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
