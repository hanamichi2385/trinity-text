using AutoMapper;
using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.Business.Services.Impl
{
    public class PageService : IPageService
    {
        private readonly IRepository<Page> _pageRepository;

        private readonly ILogger<PageService> _logger;

        private readonly IMapper _mapper;

        public PageService(IRepository<Page> pageRepository, IMapper mapper, ILogger<PageService> logger)
        {
            _pageRepository = pageRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<PagedResult<PageDTO>>> Search(SearchPageDTO search, int page, int size)
        {
            try
            {
                var query =
                    GetPagesByFilter(search);

                var totalCount = query.Count();

                var list = query
                    .GetPage(page, size)
                    .ToList();

                var result = new PagedResult<PageDTO>()
                {
                    Page = page,
                    PageSize = size,
                    Result = _mapper.Map<IList<PageDTO>>(list),
                    TotalCount = totalCount,
                };

                return await Task.FromResult(OperationResult<PagedResult<PageDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("SEARCH", ex);
                return OperationResult<PagedResult<PageDTO>>.MakeFailure(new[] { ErrorMessage.Create("SEARCH", "GENERIC_ERROR") });
            }
        }

        private IQueryable<Page> GetPagesByFilter(SearchPageDTO search)
        {
            var websites = search.UserWebsites;
            var languages = search.UserWebsites;

            var query =
                _pageRepository
                .Repository
                .Where(s =>
                    (string.IsNullOrWhiteSpace(s.FK_WEBSITE) ||
                    (!string.IsNullOrWhiteSpace(s.FK_WEBSITE) && websites.Contains(s.FK_WEBSITE, StringComparer.InvariantCultureIgnoreCase)))
                    && languages.Contains(s.FK_LANGUAGE, StringComparer.InvariantCultureIgnoreCase));

            if (search != null)
            {
                if (search.PageTypeId.HasValue)
                {
                    var typeId = search.PageTypeId.Value;
                    if (typeId != -1)
                    {
                        query =
                            query
                            .Where(s => s.FK_PAGETYPE == typeId);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search.Website))
                {
                    query =
                        query
                        .Where(s =>
                        (string.IsNullOrWhiteSpace(s.FK_WEBSITE) ||
                        (!string.IsNullOrWhiteSpace(s.FK_WEBSITE) && s.FK_WEBSITE == search.Website)));
                }

                if (!string.IsNullOrWhiteSpace(search.Site))
                {
                    query =
                        query
                        .Where(s =>
                        (string.IsNullOrWhiteSpace(s.FK_PRICELIST) ||
                        (!string.IsNullOrWhiteSpace(s.FK_PRICELIST) && s.FK_PRICELIST == search.Site)));
                }

                if (search.LanguageIds?.Any() ?? false)
                {
                    query =
                        query.Where(r => search.LanguageIds.Contains(r.FK_LANGUAGE));
                }

                if (!string.IsNullOrWhiteSpace(search.Terms))
                {
                    query =
                        query
                        .Where(s => s.TITLE.Contains(search.Terms, StringComparison.InvariantCultureIgnoreCase));
                }

                if (search.ShowOnlyActive.HasValue)
                {
                    query =
                        query.Where(r => r.ACTIVE == search.ShowOnlyActive.Value);
                }

                if ((!search.SortingName.HasValue && !search.SortingWebsite.HasValue && !search.SortingSite.HasValue && !search.SortingLanguage.HasValue) ||
                    (search.SortingName.Value == SortingType.Unordered && search.SortingWebsite.Value == SortingType.Unordered && search.SortingSite.Value == SortingType.Unordered && search.SortingLanguage.Value == SortingType.Unordered))
                {
                    query = query.Sort((r) => r.TITLE, SortingType.Ascending);
                }
                else
                {
                    query = query.Sort((r) => r.TITLE, search.SortingName);
                    query = query.Sort((r) => r.FK_WEBSITE, search.SortingWebsite);
                    query = query.Sort((r) => r.FK_PRICELIST, search.SortingSite);
                    query = query.Sort((r) => r.FK_LANGUAGE, search.SortingLanguage);
                }
            }

            return query;
        }

        public async Task<OperationResult<PageDTO>> Get(int id)
        {
            try
            {
                var entity = await _pageRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = _mapper.Map<PageDTO>(entity);

                    return OperationResult<PageDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<PageDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<PageDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<PageDTO>> Save(PageDTO dto)
        {
            try
            {
                if (dto.Id.HasValue)
                {
                    var entity = await _pageRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.CONTENT = dto.Content;
                        entity.FK_PRICELIST = dto.Site;
                        entity.FK_WEBSITE = dto.Website;
                        entity.FK_LANGUAGE = dto.Language;
                        entity.FK_PAGETYPE = dto.PageTypeId;
                        entity.TITLE = dto.Title;
                        entity.ACTIVE = dto.Active;
                        entity.GENERATE_PDF = dto.GeneratePdf;
                        entity.LASTUPDATE_USER = dto.LastUpdateUser;
                        entity.LASTUPDATE_DATE = DateTime.Now;

                        var result = await _pageRepository.Update(entity);

                        var r = _mapper.Map<PageDTO>(result);
                        return OperationResult<PageDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<PageDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                    }
                }
                else
                {
                    var existRs = await NotDuplicated(dto);

                    if (existRs.Success)
                    {
                        var entity = _mapper.Map<Page>(dto);
                        entity.ACTIVE = true;
                        entity.CREATION_USER = entity.CREATION_USER;
                        entity.CREATION_DATE = DateTime.Now;
                        await _pageRepository.Create(entity);

                        var r = _mapper.Map<PageDTO>(entity);

                        return OperationResult<PageDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<PageDTO>.MakeFailure(existRs.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<PageDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
            }
        }

        private async Task<OperationResult> NotDuplicated(PageDTO dto)
        {
            try
            {
                var query =
                    _pageRepository.Repository
                        .Where(r =>
                        r.FK_PAGETYPE == dto.PageTypeId
                            && r.TITLE == dto.Title
                            && r.FK_LANGUAGE == dto.Language
                            );

                if (!string.IsNullOrWhiteSpace(dto.Website))
                {
                    query =
                        query.Where(r => r.FK_WEBSITE == dto.Website);
                }
                else
                {
                    query =
                        query.Where(r => r.FK_WEBSITE == null);
                }

                if (!string.IsNullOrWhiteSpace(dto.Site))
                {
                    query =
                        query.Where(r => r.FK_PRICELIST == dto.Site);
                }
                else
                {
                    query =
                        query.Where(r => r.FK_PRICELIST == null);
                }

                var resx = query.Count();

                return await Task.FromResult(resx == 0 ? OperationResult.MakeSuccess() : OperationResult.MakeFailure(new[] { ErrorMessage.Create("DUPLICATED", "DUPLICATED") }));
            }
            catch (Exception ex)
            {
                _logger.LogError("EXIST", ex);
                return OperationResult<TextDTO>.MakeFailure(new[] { ErrorMessage.Create("EXIST", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<Dictionary<string, List<PageDTO>>>> GetPublishablePages(string website, string site, string[] languages)
        {
            try
            {
                var search = new SearchPageDTO()
                {
                    Website = website,
                    Site = site,
                    LanguageIds = languages,
                    ShowOnlyActive = true,
                };

                var query =
                    GetPagesByFilter(search);

                var contents = query.ToList();

                var list = _mapper.Map<List<PageDTO>>(contents);

                var result = list.GroupBy(c => c.Language).ToDictionary(c=> c.Key, c=>c.ToList());


                return await Task.FromResult(OperationResult<Dictionary<string, List<PageDTO>>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("SEARCH", ex);
                return OperationResult<Dictionary<string, List<PageDTO>>>.MakeFailure(new[] { ErrorMessage.Create("PUBLISH_PAGES", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _pageRepository
                    .Read(id);

                if (entity != null)
                {
                    await _pageRepository.Delete(entity);

                    return OperationResult.MakeSuccess();
                }
                else
                {
                    return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("REMOVE", ex);
                return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "GENERIC_ERROR") });
            }
        }
    }
}
