using Riok.Mapperly.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using TrinityText.Domain;

namespace TrinityText.Business
{
    /// <summary>
    /// Entity &lt;-&gt; DTO mapping, generated at build time (no reflection, no runtime graph walking).
    /// A new unmapped DTO/entity member is a build error (see RMG012 in the project file).
    /// </summary>
    [Mapper(PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
    internal static partial class BusinessMapper
    {
        // ---- TextType

        [MapProperty(nameof(TextType.CONTENTTYPE), nameof(TextTypeDTO.Name))]
        [MapperIgnoreTarget(nameof(TextTypeDTO.TextNumbers))]
        public static partial TextTypeDTO ToDto(TextType source);

        [MapProperty(nameof(TextTypeDTO.Name), nameof(TextType.CONTENTTYPE))]
        [MapperIgnoreTarget(nameof(TextType.TEXTS))]
        [MapperIgnoreTarget(nameof(TextType.TEXTTYPEPERWEBSITES))]
        public static partial TextType ToEntity(TextTypeDTO source);

        public static partial List<TextTypeDTO> ToDtoList(IEnumerable<TextType> source);

        // ---- Text

        [MapProperty(nameof(Text.FK_PRICELIST), nameof(TextDTO.Site))]
        [MapProperty(nameof(Text.FK_WEBSITE), nameof(TextDTO.Website))]
        [MapProperty(nameof(Text.FK_TEXTTYPE), nameof(TextDTO.TextTypeId))]
        [MapProperty(nameof(Text.FK_COUNTRY), nameof(TextDTO.Country))]
        [MapProperty(nameof(Text.FK_LANGUAGE), nameof(TextDTO.Language))]
        [MapperIgnoreTarget(nameof(TextDTO.Name))]
        [MapperIgnoreTarget(nameof(TextDTO.TextRevision))]
        private static partial TextDTO ToDtoCore(Text source);

        [UserMapping(Default = true)]
        public static TextDTO ToDto(Text source)
        {
            if (source == null)
            {
                return null;
            }

            var target = ToDtoCore(source);
            target.Name = source.NAME?.ToUpperInvariant();
            target.TextRevision = ToDto(GetTextRevision(source.REVISIONS));
            return target;
        }

        // the type is linked through FK_TEXTTYPE: mapping the nested DTO would try to insert a TextType
        [MapProperty(nameof(TextDTO.Site), nameof(Text.FK_PRICELIST))]
        [MapProperty(nameof(TextDTO.Website), nameof(Text.FK_WEBSITE))]
        [MapProperty(nameof(TextDTO.TextTypeId), nameof(Text.FK_TEXTTYPE))]
        [MapProperty(nameof(TextDTO.Country), nameof(Text.FK_COUNTRY))]
        [MapProperty(nameof(TextDTO.Language), nameof(Text.FK_LANGUAGE))]
        [MapperIgnoreTarget(nameof(Text.TEXTTYPE))]
        [MapperIgnoreTarget(nameof(Text.NAME))]
        [MapperIgnoreTarget(nameof(Text.REVISIONS))]
        private static partial Text ToEntityCore(TextDTO source);

        [UserMapping(Default = true)]
        public static Text ToEntity(TextDTO source)
        {
            if (source == null)
            {
                return null;
            }

            var target = ToEntityCore(source);
            target.NAME = source.Name?.ToUpperInvariant();
            target.REVISIONS = new[] { ToEntity(source.TextRevision) };
            return target;
        }

        public static partial List<TextDTO> ToDtoList(IEnumerable<Text> source);

        // ---- TextRevision

        [MapProperty(nameof(TextRevision.REVISION_NUMBER), nameof(TextRevisionDTO.Index))]
        [MapProperty(nameof(TextRevision.CREATION_DATE), nameof(TextRevisionDTO.CreationDate))]
        [MapProperty(nameof(TextRevision.CREATION_USER), nameof(TextRevisionDTO.CreationUser))]
        [MapperIgnoreTarget(nameof(TextRevisionDTO.Content))]
        private static partial TextRevisionDTO ToDtoCore(TextRevision source);

        [UserMapping(Default = true)]
        public static TextRevisionDTO ToDto(TextRevision source)
        {
            if (source == null)
            {
                return null;
            }

            var target = ToDtoCore(source);
            target.Content = string.IsNullOrWhiteSpace(source.CONTENT) ? string.Empty : source.CONTENT;
            return target;
        }

        [MapProperty(nameof(TextRevisionDTO.Index), nameof(TextRevision.REVISION_NUMBER))]
        [MapProperty(nameof(TextRevisionDTO.CreationDate), nameof(TextRevision.CREATION_DATE))]
        [MapProperty(nameof(TextRevisionDTO.CreationUser), nameof(TextRevision.CREATION_USER))]
        [MapperIgnoreTarget(nameof(TextRevision.CONTENT))]
        [MapperIgnoreTarget(nameof(TextRevision.FK_TEXT))]
        [MapperIgnoreTarget(nameof(TextRevision.TEXT))]
        private static partial TextRevision ToEntityCore(TextRevisionDTO source);

        [UserMapping(Default = true)]
        public static TextRevision ToEntity(TextRevisionDTO source)
        {
            if (source == null)
            {
                return null;
            }

            var target = ToEntityCore(source);
            target.CONTENT = string.IsNullOrWhiteSpace(source.Content) ? string.Empty : source.Content;
            return target;
        }

        public static partial List<TextRevisionDTO> ToDtoList(IEnumerable<TextRevision> source);

        // ---- PageType

        [MapProperty(nameof(PageType.FK_WEBSITE), nameof(PageTypeDTO.Website))]
        [MapProperty(nameof(PageType.OUTPUT_FILENAME), nameof(PageTypeDTO.OutputFilename))]
        [MapProperty(nameof(PageType.PRINT_ELEMENT_NAME), nameof(PageTypeDTO.PrintElementName))]
        [MapperIgnoreTarget(nameof(PageTypeDTO.PathPreviewPage))]
        [MapperIgnoreTarget(nameof(PageTypeDTO.PageTotals))]
        [MapperIgnoreTarget(nameof(PageTypeDTO.Visibility))]
        private static partial PageTypeDTO ToDtoCore(PageType source);

        [UserMapping(Default = true)]
        public static PageTypeDTO ToDto(PageType source)
        {
            if (source == null)
            {
                return null;
            }

            var target = ToDtoCore(source);
            target.PageTotals = source.PAGES != null ? source.PAGES.Count : 0;
            target.Visibility = string.IsNullOrWhiteSpace(source.VISIBILITY)
                ? new List<string>()
                : source.VISIBILITY.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
            return target;
        }

        [MapProperty(nameof(PageTypeDTO.Website), nameof(PageType.FK_WEBSITE))]
        [MapProperty(nameof(PageTypeDTO.OutputFilename), nameof(PageType.OUTPUT_FILENAME))]
        [MapProperty(nameof(PageTypeDTO.PrintElementName), nameof(PageType.PRINT_ELEMENT_NAME))]
        [MapperIgnoreTarget(nameof(PageType.PATH_PREVIEWPAGE))]
        [MapperIgnoreTarget(nameof(PageType.PAGES))]
        [MapperIgnoreTarget(nameof(PageType.VISIBILITY))]
        private static partial PageType ToEntityCore(PageTypeDTO source);

        [UserMapping(Default = true)]
        public static PageType ToEntity(PageTypeDTO source)
        {
            if (source == null)
            {
                return null;
            }

            var target = ToEntityCore(source);
            target.VISIBILITY = string.Join("|", source.Visibility ?? Array.Empty<string>());
            return target;
        }

        public static partial List<PageTypeDTO> ToDtoList(IEnumerable<PageType> source);

        // ---- Page

        [MapProperty(nameof(Page.FK_WEBSITE), nameof(PageDTO.Website))]
        [MapProperty(nameof(Page.FK_PRICELIST), nameof(PageDTO.Site))]
        [MapProperty(nameof(Page.FK_LANGUAGE), nameof(PageDTO.Language))]
        [MapProperty(nameof(Page.FK_PAGETYPE), nameof(PageDTO.PageTypeId))]
        [MapProperty(nameof(Page.LASTUPDATE_DATE), nameof(PageDTO.LastUpdate))]
        [MapProperty(nameof(Page.LASTUPDATE_USER), nameof(PageDTO.LastUpdateUser))]
        [MapProperty(nameof(Page.CREATION_DATE), nameof(PageDTO.CreationDate))]
        [MapProperty(nameof(Page.CREATION_USER), nameof(PageDTO.CreationUser))]
        [MapProperty(nameof(Page.PAGETYPE), nameof(PageDTO.PageType))]
        [MapperIgnoreTarget(nameof(PageDTO.GeneratePdf))]
        public static partial PageDTO ToDto(Page source);

        [MapProperty(nameof(PageDTO.Website), nameof(Page.FK_WEBSITE))]
        [MapProperty(nameof(PageDTO.Site), nameof(Page.FK_PRICELIST))]
        [MapProperty(nameof(PageDTO.Language), nameof(Page.FK_LANGUAGE))]
        [MapProperty(nameof(PageDTO.PageTypeId), nameof(Page.FK_PAGETYPE))]
        [MapProperty(nameof(PageDTO.LastUpdate), nameof(Page.LASTUPDATE_DATE))]
        [MapProperty(nameof(PageDTO.LastUpdateUser), nameof(Page.LASTUPDATE_USER))]
        [MapProperty(nameof(PageDTO.CreationDate), nameof(Page.CREATION_DATE))]
        [MapProperty(nameof(PageDTO.CreationUser), nameof(Page.CREATION_USER))]
        [MapProperty(nameof(PageDTO.PageType), nameof(Page.PAGETYPE))]
        [MapperIgnoreTarget(nameof(Page.GENERATE_PDF))]
        public static partial Page ToEntity(PageDTO source);

        public static partial List<PageDTO> ToDtoList(IEnumerable<Page> source);

        // ---- Widget

        [MapProperty(nameof(Widget.FK_WEBSITE), nameof(WidgetDTO.Website))]
        [MapProperty(nameof(Widget.FK_PRICELIST), nameof(WidgetDTO.Site))]
        [MapProperty(nameof(Widget.FK_LANGUAGE), nameof(WidgetDTO.Language))]
        [MapProperty(nameof(Widget.LASTUPDATE_DATE), nameof(WidgetDTO.LastUpdate))]
        [MapProperty(nameof(Widget.LASTUPDATE_USER), nameof(WidgetDTO.LastUpdateUser))]
        [MapProperty(nameof(Widget.CREATION_DATE), nameof(WidgetDTO.CreationDate))]
        [MapProperty(nameof(Widget.CREATION_USER), nameof(WidgetDTO.CreationUser))]
        public static partial WidgetDTO ToDto(Widget source);

        [MapProperty(nameof(WidgetDTO.Website), nameof(Widget.FK_WEBSITE))]
        [MapProperty(nameof(WidgetDTO.Site), nameof(Widget.FK_PRICELIST))]
        [MapProperty(nameof(WidgetDTO.Language), nameof(Widget.FK_LANGUAGE))]
        [MapProperty(nameof(WidgetDTO.LastUpdate), nameof(Widget.LASTUPDATE_DATE))]
        [MapProperty(nameof(WidgetDTO.LastUpdateUser), nameof(Widget.LASTUPDATE_USER))]
        [MapProperty(nameof(WidgetDTO.CreationDate), nameof(Widget.CREATION_DATE))]
        [MapProperty(nameof(WidgetDTO.CreationUser), nameof(Widget.CREATION_USER))]
        public static partial Widget ToEntity(WidgetDTO source);

        public static partial List<WidgetDTO> ToDtoList(IEnumerable<Widget> source);

        // ---- Folder

        [MapProperty(nameof(Folder.FK_WEBSITE), nameof(FolderDTO.Website))]
        [MapProperty(nameof(Folder.FK_PARENT), nameof(FolderDTO.ParentId))]
        [MapperIgnoreTarget(nameof(FolderDTO.SubFolders))]
        public static partial FolderDTO ToDto(Folder source);

        [MapProperty(nameof(FolderDTO.Website), nameof(Folder.FK_WEBSITE))]
        [MapProperty(nameof(FolderDTO.ParentId), nameof(Folder.FK_PARENT))]
        [MapperIgnoreSource(nameof(FolderDTO.SubFolders))]
        public static partial Folder ToEntity(FolderDTO source);

        public static partial List<FolderDTO> ToDtoList(IEnumerable<Folder> source);

        // ---- File

        [MapProperty(nameof(File.LASTUPDATE_DATE), nameof(FileDTO.LastUpdate))]
        [MapProperty(nameof(File.LASTUPDATE_USER), nameof(FileDTO.LastUpdateUser))]
        [MapProperty(nameof(File.CREATION_DATE), nameof(FileDTO.CreationDate))]
        [MapProperty(nameof(File.CREATION_USER), nameof(FileDTO.CreationUser))]
        [MapperIgnoreTarget(nameof(FileDTO.HasThumbnail))]
        [MapperIgnoreTarget(nameof(FileDTO.Size))]
        private static partial FileDTO ToDtoCore(File source);

        [UserMapping(Default = true)]
        public static FileDTO ToDto(File source)
        {
            if (source == null)
            {
                return null;
            }

            var target = ToDtoCore(source);
            target.HasThumbnail = source.THUMBNAIL != null;
            return target;
        }

        [MapProperty(nameof(FileDTO.LastUpdate), nameof(File.LASTUPDATE_DATE))]
        [MapProperty(nameof(FileDTO.LastUpdateUser), nameof(File.LASTUPDATE_USER))]
        [MapProperty(nameof(FileDTO.CreationDate), nameof(File.CREATION_DATE))]
        [MapProperty(nameof(FileDTO.CreationUser), nameof(File.CREATION_USER))]
        [MapperIgnoreTarget(nameof(File.THUMBNAIL))]
        [MapperIgnoreTarget(nameof(File.FK_FOLDER))]
        [MapperIgnoreTarget(nameof(File.FK_WEBSITE))]
        public static partial File ToEntity(FileDTO source);

        public static partial List<FileDTO> ToDtoList(IEnumerable<File> source);

        // ---- CacheSettings

        [MapProperty(nameof(CacheSettings.FK_CDNSERVER), nameof(CacheSettingsDTO.CdnServerId))]
        [MapProperty(nameof(CacheSettings.CDNSERVER), nameof(CacheSettingsDTO.CdnServer))]
        public static partial CacheSettingsDTO ToDto(CacheSettings source);

        [MapProperty(nameof(CacheSettingsDTO.CdnServerId), nameof(CacheSettings.FK_CDNSERVER))]
        [MapProperty(nameof(CacheSettingsDTO.CdnServer), nameof(CacheSettings.CDNSERVER))]
        public static partial CacheSettings ToEntity(CacheSettingsDTO source);

        public static partial List<CacheSettingsDTO> ToDtoList(IEnumerable<CacheSettings> source);

        // ---- WebsiteConfiguration

        [MapProperty(nameof(WebsiteConfiguration.FK_WEBSITE), nameof(WebsiteConfigurationDTO.Website))]
        [MapProperty(nameof(WebsiteConfiguration.TYPE), nameof(WebsiteConfigurationDTO.Type))]
        [MapperIgnoreTarget(nameof(WebsiteConfigurationDTO.IdType))]
        public static partial WebsiteConfigurationDTO ToDto(WebsiteConfiguration source);

        [MapProperty(nameof(WebsiteConfigurationDTO.Website), nameof(WebsiteConfiguration.FK_WEBSITE))]
        [MapProperty(nameof(WebsiteConfigurationDTO.Type), nameof(WebsiteConfiguration.TYPE))]
        [MapperIgnoreSource(nameof(WebsiteConfigurationDTO.IdType))]
        public static partial WebsiteConfiguration ToEntity(WebsiteConfigurationDTO source);

        public static partial List<WebsiteConfigurationDTO> ToDtoList(IEnumerable<WebsiteConfiguration> source);

        // ---- FtpServer / CdnServer

        public static partial FTPServerDTO ToDto(FtpServer source);

        [MapperIgnoreTarget(nameof(FtpServer.PUBLICATIONS))]
        [MapperIgnoreTarget(nameof(FtpServer.CDNSERVERS))]
        public static partial FtpServer ToEntity(FTPServerDTO source);

        public static partial List<FTPServerDTO> ToDtoList(IEnumerable<FtpServer> source);

        [MapperIgnoreTarget(nameof(CdnServerDTO.FtpServers))]
        private static partial CdnServerDTO ToDtoCore(CdnServer source);

        [UserMapping(Default = true)]
        public static CdnServerDTO ToDto(CdnServer source)
        {
            if (source == null)
            {
                return null;
            }

            var target = ToDtoCore(source);
            target.FtpServers = source.FTPSERVERS?.Select(f => ToDto(f.FTPSERVER)).ToList();
            return target;
        }

        [MapperIgnoreTarget(nameof(CdnServer.CDNSERVERPERWEBSITES))]
        [MapperIgnoreTarget(nameof(CdnServer.PUBLICATIONS))]
        [MapperIgnoreTarget(nameof(CdnServer.FTPSERVERS))]
        [MapperIgnoreTarget(nameof(CdnServer.CACHESETTINGS))]
        public static partial CdnServer ToEntity(CdnServerDTO source);

        public static partial List<CdnServerDTO> ToDtoList(IEnumerable<CdnServer> source);

        // ---- Publication

        [MapProperty(nameof(Publication.CDNSERVER), nameof(PublicationDTO.CdnServer))]
        [MapProperty(nameof(Publication.FTPSERVER), nameof(PublicationDTO.FtpServer))]
        [MapProperty(nameof(Publication.DATATYPE), nameof(PublicationDTO.DataType))]
        [MapProperty(nameof(Publication.FILTERDATA_DATE), nameof(PublicationDTO.FilterDataDate))]
        [MapProperty(nameof(Publication.LASTUPDATE_DATE), nameof(PublicationDTO.LastUpdate))]
        [MapProperty(nameof(Publication.MANUALDELETE), nameof(PublicationDTO.ManualDelete))]
        [MapProperty(nameof(Publication.STATUS_CODE), nameof(PublicationDTO.StatusCode))]
        [MapProperty(nameof(Publication.STATUS_MESSAGE), nameof(PublicationDTO.StatusMessage))]
        [MapProperty(nameof(Publication.FK_WEBSITE), nameof(PublicationDTO.Website))]
        [MapProperty(nameof(Publication.CREATION_USER), nameof(PublicationDTO.CreationUser))]
        [MapperIgnoreTarget(nameof(PublicationDTO.Payload))]
        [MapperIgnoreTarget(nameof(PublicationDTO.ZipFile))]
        [MapperIgnoreTarget(nameof(PublicationDTO.HasZipFile))]
        public static partial PublicationDTO ToDto(Publication source);

        private static TextRevision GetTextRevision(ICollection<TextRevision> revisions)
        {
            if (revisions == null || revisions.Count == 0)
            {
                return null;
            }
            return revisions.MaxBy(rev => rev.REVISION_NUMBER);
        }
    }
}
