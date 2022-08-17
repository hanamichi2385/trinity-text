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
                .ForMember(d => d.TextType, src => src.MapFrom(s => s.TEXTTYPE))
                .ForMember(d => d.TextRevision, src => src.MapFrom(s => GetTextRevision(s.REVISIONS)));

            CreateMap<TextDTO, Text>()
                .ForMember(d => d.FK_PRICELIST, src => src.MapFrom(s => s.Site))
                .ForMember(d => d.FK_WEBSITE, src => src.MapFrom(s => s.Website))
                .ForMember(d => d.FK_TEXTTYPE, src => src.MapFrom(s => s.TextTypeId))
                .ForMember(d => d.FK_COUNTRY, src => src.MapFrom(s => s.Country))
                .ForMember(d => d.TEXTTYPE, src => src.MapFrom(s => s.TextType))
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
                .ForMember(d => d.HasSubfolder, src => src.Ignore());

            CreateMap<PageTypeDTO, PageType>()
                .ForMember(d => d.FK_WEBSITE, src => src.MapFrom(s => s.Website));

            CreateMap<Page, PageDTO>()
                .ReverseMap();
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
