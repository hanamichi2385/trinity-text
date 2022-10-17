using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using TrinityText.Domain;

namespace TrinityText.Business
{
    public class BusinessMapperProfile : Profile
    {
        public BusinessMapperProfile()
        {
            CreateMap<TextType, TextTypeDTO>()
                .ForMember(d => d.Name, src => src.MapFrom(s => s.CONTENTTYPE))
                .ReverseMap();

            CreateMap<Text, TextDTO>()
                .ForMember(d => d.Site, src => src.MapFrom(s => s.FK_PRICELIST))
                .ForMember(d => d.Website, src => src.MapFrom(s => s.FK_WEBSITE))
                .ForMember(d => d.TextTypeId, src => src.MapFrom(s => s.FK_TEXTTYPE))
                .ForMember(d => d.Country, src => src.MapFrom(s => s.FK_COUNTRY))
                .ForMember(d => d.Language, src => src.MapFrom(s => s.FK_LANGUAGE))
                .ForMember(d => d.TextType, src => src.MapFrom(s => s.TEXTTYPE))
                .ForMember(d => d.Name, src => src.MapFrom(s => s.NAME.ToUpper()))
                .ForMember(d => d.TextRevision, src => src.MapFrom(s => GetTextRevision(s.REVISIONS)));

            CreateMap<TextDTO, Text>()
                .ForMember(d => d.FK_PRICELIST, src => src.MapFrom(s => s.Site))
                .ForMember(d => d.FK_WEBSITE, src => src.MapFrom(s => s.Website))
                .ForMember(d => d.FK_TEXTTYPE, src => src.MapFrom(s => s.TextTypeId))
                .ForMember(d => d.FK_COUNTRY, src => src.MapFrom(s => s.Country))
                .ForMember(d => d.FK_LANGUAGE, src => src.MapFrom(s => s.Language))
                .ForMember(d => d.TEXTTYPE, src => src.MapFrom(s => s.TextType))
                .ForMember(d => d.NAME, src => src.MapFrom(s => s.Name.ToUpper()))
                .ForMember(d => d.REVISIONS, src => src.MapFrom(s => new[] { s.TextRevision }));

            CreateMap<TextRevision, TextRevisionDTO>()
                .ForMember(d => d.Content, src => src.MapFrom(s => s.CONTENT))
                .ForMember(d => d.CreationDate, src => src.MapFrom(s => s.CREATION_DATE))
                .ForMember(d => d.CreationUser, src => src.MapFrom(s => s.CREATION_USER))
                .ForMember(d => d.Id, src => src.MapFrom(s => s.ID))
                .ForMember(d => d.Index, src => src.MapFrom(s => s.REVISION_NUMBER))
                .ReverseMap();

            CreateMap<PageType, PageTypeDTO>()
                .ForMember(d => d.PageTotals, src => src.MapFrom(s => s.PAGES != null ? s.PAGES.Count : 0))
                .ForMember(d => d.Visibility, src => src.MapFrom(s => string.IsNullOrWhiteSpace(s.VISIBILITY) ? new List<string>() : s.VISIBILITY.Split('|', System.StringSplitOptions.RemoveEmptyEntries).ToList()))
                .ForMember(d => d.Website, src => src.MapFrom(s => s.FK_WEBSITE))
                .ForMember(d => d.OutputFilename, src => src.MapFrom(s => s.OUTPUT_FILENAME))
                .ForMember(d => d.Name, src => src.MapFrom(s => s.NAME))
                .ForMember(d => d.PrintElementName, src => src.MapFrom(s => s.PRINT_ELEMENT_NAME))
                .ForMember(d => d.Visibility, src => src.MapFrom(s => s.VISIBILITY.Split(',', System.StringSplitOptions.RemoveEmptyEntries)))
                .ForMember(d => d.HasSubfolder, src => src.Ignore());

            CreateMap<PageTypeDTO, PageType>()
                .ForMember(d => d.FK_WEBSITE, src => src.MapFrom(s => s.Website))
                .ForMember(d => d.OUTPUT_FILENAME, src => src.MapFrom(s => s.OutputFilename))
                .ForMember(d => d.NAME, src => src.MapFrom(s => s.Name))
                .ForMember(d => d.PRINT_ELEMENT_NAME, src => src.MapFrom(s => s.PrintElementName))
                .ForMember(d => d.VISIBILITY, src => src.MapFrom(s => string.Join(",", s.Visibility)));

            CreateMap<Page, PageDTO>()
                .ForMember(d => d.CreationDate, src => src.MapFrom(s => s.CREATION_DATE))
                .ForMember(d => d.CreationUser, src => src.MapFrom(s => s.CREATION_USER))
                .ForMember(d => d.LastUpdate, src => src.MapFrom(s => s.LASTUPDATE_DATE))
                .ForMember(d => d.Website, src => src.MapFrom(s => s.FK_WEBSITE))
                .ForMember(d => d.Site, src => src.MapFrom(s => s.FK_PRICELIST))
                .ForMember(d => d.Language, src => src.MapFrom(s => s.FK_LANGUAGE))
                .ForMember(d => d.LastUpdateUser, src => src.MapFrom(s => s.LASTUPDATE_USER))
                .ForMember(d => d.PageTypeId, src => src.MapFrom(s => s.FK_PAGETYPE));

            CreateMap<PageDTO, Page>()
                .ForMember(d => d.CREATION_DATE, src => src.MapFrom(s => s.CreationDate))
                .ForMember(d => d.CREATION_USER, src => src.MapFrom(s => s.CreationUser))
                .ForMember(d => d.LASTUPDATE_DATE, src => src.MapFrom(s => s.LastUpdate))
                .ForMember(d => d.FK_WEBSITE, src => src.MapFrom(s => s.Website))
                .ForMember(d => d.FK_PRICELIST, src => src.MapFrom(s => s.Site))
                .ForMember(d => d.FK_LANGUAGE, src => src.MapFrom(s => s.Language))
                .ForMember(d => d.LASTUPDATE_USER, src => src.MapFrom(s => s.LastUpdateUser))
                .ForMember(d => d.FK_PAGETYPE, src => src.MapFrom(s => s.PageTypeId));

            CreateMap<Widget, WidgetDTO>()
                .ForMember(d => d.CreationDate, src => src.MapFrom(s => s.CREATION_DATE))
                .ForMember(d => d.CreationUser, src => src.MapFrom(s => s.CREATION_USER))
                .ForMember(d => d.LastUpdate, src => src.MapFrom(s => s.LASTUPDATE_DATE))
                .ForMember(d => d.LastUpdateUser, src => src.MapFrom(s => s.LASTUPDATE_USER))
                .ForMember(d => d.Website, src => src.MapFrom(s => s.FK_WEBSITE))
                .ForMember(d => d.Site, src => src.MapFrom(s => s.FK_PRICELIST))
                .ForMember(d => d.Language, src => src.MapFrom(s => s.FK_LANGUAGE))
                .ReverseMap();

            CreateMap<Folder, FolderDTO>()
                .ForMember(d => d.Website, src => src.MapFrom(s => s.FK_WEBSITE))
                //.ForMember(d => d.ParentFolder, src => src.Ignore())
                .ForMember(d => d.ParentId, src => src.MapFrom(s => s.FK_PARENT))
                .ForMember(d => d.SubFolders, src => src.Ignore());

            CreateMap<FolderDTO, Folder>()
                .ForMember(d => d.FK_WEBSITE, src => src.MapFrom(s => s.Website))
                .ForMember(d => d.FK_PARENT, src => src.MapFrom(s => s.ParentId))
                //.ForMember(d => d.PARENT, src => src.MapFrom(s => s.ParentFolder))
                //.ForMember(d => d.SUBFOLDERS, src => src.Ignore())
                //.ForMember(d => d.FILES, src => src.Ignore());
                ;
                

            CreateMap<File, FileDTO>()
                .ForMember(d => d.CreationDate, src => src.MapFrom(s => s.CREATION_DATE))
                .ForMember(d => d.CreationUser, src => src.MapFrom(s => s.CREATION_USER))
                .ForMember(d => d.LastUpdate, src => src.MapFrom(s => s.LASTUPDATE_DATE))
                .ForMember(d => d.LastUpdateUser, src => src.MapFrom(s => s.LASTUPDATE_USER))
                .ForMember(d => d.HasThumbnail, src => src.MapFrom(s => s.THUMBNAIL != null))
                .ForMember(d => d.Content, src => src.MapFrom(s => s.CONTENT));

            CreateMap<FileDTO, File>()
                .ForMember(d => d.CREATION_DATE, src => src.MapFrom(s => s.CreationDate))
                .ForMember(d => d.CREATION_USER, src => src.MapFrom(s => s.CreationUser))
                .ForMember(d => d.LASTUPDATE_DATE, src => src.MapFrom(s => s.LastUpdate))
                .ForMember(d => d.LASTUPDATE_USER, src => src.MapFrom(s => s.LastUpdateUser));

            CreateMap<CacheSettings, CacheSettingsDTO>()
                .IncludeAllDerived()
                .ForMember(d => d.CdnServerId, src => src.MapFrom(s => s.FK_CDNSERVER))
                .ReverseMap();

            CreateMap<WebsiteConfiguration, WebsiteConfigurationDTO>()
                .ForMember(d => d.Website, src => src.MapFrom(s => s.FK_WEBSITE))
                .ReverseMap();

            CreateMap<FtpServer, FTPServerDTO>()
                .ReverseMap();

            CreateMap<CdnServer, CdnServerDTO>()
                .ForMember(d => d.FtpServers, src => src.MapFrom(s => s.FTPSERVERS.Select(f => f.FTPSERVER)))
                .ReverseMap();

            CreateMap<Publication, PublicationDTO>()
                .ForMember(d => d.CdnServer, src => src.MapFrom(s => s.CDNSERVER))
                .ForMember(d => d.CreationUser, src => src.MapFrom(s => s.CREATION_USER))
                .ForMember(d => d.DataType, src => src.MapFrom(s => s.DATATYPE))
                .ForMember(d => d.Email, src => src.MapFrom(s => s.EMAIL))
                .ForMember(d => d.FilterDataDate, src => src.MapFrom(s => s.FILTERDATA_DATE))
                .ForMember(d => d.Format, src => src.MapFrom(s => s.FORMAT))
                .ForMember(d => d.FtpServer, src => src.MapFrom(s => s.FTPSERVER))
                .ForMember(d => d.Id, src => src.MapFrom(s => s.ID))
                .ForMember(d => d.LastUpdate, src => src.MapFrom(s => s.LASTUPDATE_DATE))
                .ForMember(d => d.ManualDelete, src => src.MapFrom(s => s.MANUALDELETE))
                .ForMember(d => d.StatusCode, src => src.MapFrom(s => s.STATUS_CODE))
                .ForMember(d => d.StatusMessage, src => src.MapFrom(s => s.STATUS_MESSAGE))
                .ForMember(d => d.Website, src => src.MapFrom(s => s.FK_WEBSITE))
                .ForMember(d => d.ZipFile, src => src.MapFrom(s => s.ZIP_FILE))
                .ForMember(d => d.Payload, src => src.Ignore())
                ;
        }

        private TextRevision GetTextRevision(ICollection<TextRevision> revisions)
        {
            var revision =
               revisions
               .OrderByDescending(rev => rev.CREATION_DATE)
               .FirstOrDefault();

            return revision;
        }
    }
}
