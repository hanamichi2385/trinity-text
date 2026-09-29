using AutoMapper;
using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.Business.Services.Impl
{
    public class PageService : IPageService
    {
        private readonly IRepository<Page> _pageRepository;

        private readonly IRepository<PageType> _pageTypeRepository;

        private readonly ILogger<PageService> _logger;

        private readonly IMapper _mapper;

        public PageService(IRepository<Page> pageRepository, IRepository<PageType> pageTypeRepository, IMapper mapper, ILogger<PageService> logger)
        {
            _pageRepository = pageRepository;
            _pageTypeRepository = pageTypeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<PagedResult<PageDTO>>> Search(SearchPageDTO search, int page, int size)
        {
            try
            {
                var query = GetPagesByFilter(search);

                // page columns only: the PageType (with its schema XML) is loaded once per type, not joined on every row
                var projected = WithoutPageType(query, includeContent: !(search?.ExcludeContent ?? false));

                var totalCount = await _pageRepository.CountAsync(projected);
                var list = await _pageRepository.ToListAsync(projected.GetPage(page, size));
                await AttachPageTypes(list);

                var result = new PagedResult<PageDTO>()
                {
                    Page = page,
                    PageSize = size,
                    Result = _mapper.Map<IList<PageDTO>>(list),
                    TotalCount = totalCount,
                };

                return OperationResult<PagedResult<PageDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SEARCH {message}", ex.Message);
                return OperationResult<PagedResult<PageDTO>>.MakeFailure([ErrorMessage.Create("SEARCH", "GENERIC_ERROR")]);
            }
        }

        private IQueryable<Page> GetPagesByFilter(SearchPageDTO search, bool applySorting = true)
        {
            var websites = search.UserWebsites ?? [];
            var languages = search.WebsiteLanguages ?? [];

            var query =
                _pageRepository
                .Repository
                .Where(s =>
                    ((s.FK_WEBSITE == null || s.FK_WEBSITE == "") ||
                    ((s.FK_WEBSITE != null && s.FK_WEBSITE != "") && websites.Contains(s.FK_WEBSITE)))
                    && languages.Contains(s.FK_LANGUAGE));

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
                        ((s.FK_WEBSITE == null || s.FK_WEBSITE == "") ||
                        ((s.FK_WEBSITE != null && s.FK_WEBSITE != "") && s.FK_WEBSITE == search.Website)));
                }

                if (!string.IsNullOrWhiteSpace(search.Site))
                {
                    query =
                        query
                        .Where(s =>
                        ((s.FK_PRICELIST == null || s.FK_PRICELIST == "") ||
                        ((s.FK_PRICELIST != null && s.FK_PRICELIST != "") && s.FK_PRICELIST == search.Site)));
                }

                if ((search.LanguageIds?.Length ?? 0) != 0)
                {
                    query =
                        query.Where(r => search.LanguageIds.Contains(r.FK_LANGUAGE));
                }

                if (!string.IsNullOrWhiteSpace(search.Terms))
                {
                    query =
                        query
                        .Where(s => s.TITLE.Contains(search.Terms));
                }

                if (search.ShowOnlyActive.HasValue)
                {
                    query =
                        query.Where(r => r.ACTIVE == search.ShowOnlyActive.Value);
                }

                if (applySorting)
                {
                    var sortName = search.SortingName ?? SortingType.Unordered;
                    var sortWebsite = search.SortingWebsite ?? SortingType.Unordered;
                    var sortSite = search.SortingSite ?? SortingType.Unordered;
                    var sortLanguage = search.SortingLanguage ?? SortingType.Unordered;
                    var sortLastUpdate = search.SortingLastUpdate ?? SortingType.Unordered;

                    if (sortName == SortingType.Unordered && sortWebsite == SortingType.Unordered && sortSite == SortingType.Unordered && sortLanguage == SortingType.Unordered && sortLastUpdate == SortingType.Unordered)
                    {
                        query = query.Sort((r) => r.TITLE, SortingType.Ascending);
                    }
                    else
                    {
                        query = query.Sort((r) => r.TITLE, sortName);
                        query = query.Sort((r) => r.FK_WEBSITE, sortWebsite);
                        query = query.Sort((r) => r.FK_PRICELIST, sortSite);
                        query = query.Sort((r) => r.FK_LANGUAGE, sortLanguage);
                        query = query.Sort((r) => r.LASTUPDATE_DATE, sortLastUpdate);
                    }
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
                    return OperationResult<PageDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<PageDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        // a page is an XML document that ends up in the published files: refuse what would abort every publication
        private static bool IsValidContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return true;
            }

            if (content.Length > 5_000_000)
            {
                return false;
            }

            try
            {
                SafeXml.ParseDocument(content);
                return true;
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }
        }

        public async Task<OperationResult<PageDTO>> Save(PageDTO dto)
        {
            try
            {
                if (!IsValidContent(dto.Content))
                {
                    return OperationResult<PageDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_CONTENT")]);
                }

                if (dto.Id.HasValue)
                {
                    var id = dto.Id.Value;
                    var content = dto.Content;
                    var site = dto.Site;
                    var language = dto.Language;
                    var title = dto.Title;
                    var active = dto.Active;
                    var generatePdf = dto.GeneratePdf;
                    var user = dto.LastUpdateUser;
                    var now = DateTime.Now;

                    // website and page type never change: only the editable columns are written
                    var updated = await _pageRepository.ExecuteUpdateAsync(
                        _pageRepository.Repository.Where(p => p.ID == id),
                        set => set
                            .Set(p => p.CONTENT, content)
                            .Set(p => p.FK_PRICELIST, site)
                            .Set(p => p.FK_LANGUAGE, language)
                            .Set(p => p.TITLE, title)
                            .Set(p => p.ACTIVE, active)
                            .Set(p => p.GENERATE_PDF, generatePdf)
                            .Set(p => p.LASTUPDATE_USER, user)
                            .Set(p => p.LASTUPDATE_DATE, now));

                    if (updated > 0)
                    {
                        var saved = await _pageRepository.ToListAsync(
                            WithoutPageType(_pageRepository.Repository.Where(p => p.ID == id)));
                        await AttachPageTypes(saved);

                        return OperationResult<PageDTO>.MakeSuccess(_mapper.Map<PageDTO>(saved.Single()));
                    }
                    else
                    {
                        return OperationResult<PageDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                    }
                }
                else
                {
                    //VERIFY
                    //var existRs = await NotDuplicated(dto);

                    //if (existRs.Success)
                    //{
                    var typeId = dto.PageTypeId;
                    var pageType = await _pageTypeRepository.Read(typeId);

                    var entity = _mapper.Map<Page>(dto);
                    entity.ACTIVE = true;
                    entity.CREATION_DATE = DateTime.Now;
                    entity.LASTUPDATE_DATE = DateTime.Now;
                    entity.LASTUPDATE_USER = entity.CREATION_USER;

                    await _pageRepository.Create(entity);

                    var r = _mapper.Map<PageDTO>(entity);
                    var t = _mapper.Map<PageTypeDTO>(pageType);
                    r.PageType = t;

                    return OperationResult<PageDTO>.MakeSuccess(r);
                    //}
                    //else
                    //{
                    //    return OperationResult<PageDTO>.MakeFailure(existRs.Errors);
                    //}
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<PageDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }

        //private async Task<OperationResult> NotDuplicated(PageDTO dto)
        //{
        //    try
        //    {
        //        var query =
        //            _pageRepository.Repository
        //                .Where(r =>
        //                r.FK_PAGETYPE == dto.PageTypeId
        //                    && r.TITLE == dto.Title
        //                    && r.FK_LANGUAGE == dto.Language
        //                    );

        //        if (!string.IsNullOrWhiteSpace(dto.Website))
        //        {
        //            query =
        //                query.Where(r => r.FK_WEBSITE == dto.Website);
        //        }
        //        else
        //        {
        //            query =
        //                query.Where(r => r.FK_WEBSITE == null || r.FK_WEBSITE == "");
        //        }

        //        if (!string.IsNullOrWhiteSpace(dto.Site))
        //        {
        //            query =
        //                query.Where(r => r.FK_PRICELIST == dto.Site);
        //        }
        //        else
        //        {
        //            query =
        //                query.Where(r => r.FK_PRICELIST == null || r.FK_PRICELIST == "");
        //        }

        //        var resx = query.Count();

        //        return await Task.FromResult(resx == 0 ? OperationResult.MakeSuccess() : OperationResult.MakeFailure(new[] { ErrorMessage.Create("DUPLICATED", "DUPLICATED") }));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "EXIST {message}", ex.Message);
        //        return OperationResult<TextDTO>.MakeFailure(new[] { ErrorMessage.Create("EXIST", "GENERIC_ERROR") });
        //    }
        //}
        public async Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<PageDTO>>>> GetPublishablePages(string website, string site, string[] languages)
        {
            try
            {
                var search = new SearchPageDTO()
                {
                    Website = website,
                    Site = site,
                    LanguageIds = languages,
                    ShowOnlyActive = true,
                    UserWebsites = [website],
                    WebsiteLanguages = languages,
                };

                // the export does not need an ORDER BY on the (large) content column, nor the PageType joined on every row
                var query =
                    WithoutPageType(GetPagesByFilter(search, applySorting: false));

                var contents = await _pageRepository.ToListAsync(query);
                await AttachPageTypes(contents);

                var list = _mapper.Map<List<PageDTO>>(contents);

                var result = list.GroupBy(c => c.Language).ToFrozenDictionary(c => c.Key, c => c.ToList().AsReadOnly());

                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<PageDTO>>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUBLISH_PAGES {message}", ex.Message);
                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<PageDTO>>>.MakeFailure([ErrorMessage.Create("PUBLISH_PAGES", "GENERIC_ERROR")]);
            }
        }


        private static IQueryable<Page> WithoutPageType(IQueryable<Page> query, bool includeContent = true)
            => query.Select(q => new Page()
            {
                ACTIVE = q.ACTIVE,
                CONTENT = includeContent ? q.CONTENT : string.Empty,
                ID = q.ID,
                CREATION_DATE = q.CREATION_DATE,
                CREATION_USER = q.CREATION_USER,
                FK_LANGUAGE = q.FK_LANGUAGE,
                FK_PAGETYPE = q.FK_PAGETYPE,
                FK_PRICELIST = q.FK_PRICELIST,
                FK_WEBSITE = q.FK_WEBSITE,
                GENERATE_PDF = q.GENERATE_PDF,
                LASTUPDATE_DATE = q.LASTUPDATE_DATE,
                LASTUPDATE_USER = q.LASTUPDATE_USER,
                TITLE = q.TITLE,
            });

        // page types are few: load each one once and attach it to the pages that use it
        private async Task AttachPageTypes(IList<Page> pages)
        {
            var typeIds = pages.Select(p => p.FK_PAGETYPE).Distinct().ToArray();
            if (typeIds.Length == 0)
            {
                return;
            }

            var types = (await _pageTypeRepository.ToListAsync(
                _pageTypeRepository.Repository.Where(pt => typeIds.Contains(pt.ID))))
                .ToDictionary(pt => pt.ID);
            foreach (var page in pages)
            {
                page.PAGETYPE = types.GetValueOrDefault(page.FK_PAGETYPE);
            }
        }

        public async Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<PageDTO>>>> GetPublishablePagesByWebsite(string website, Dictionary<string, string[]> sitesLanguages)
        {
            try
            {
                var allLanguages = sitesLanguages.Values.SelectMany(v => v).Distinct().ToArray();
                var allSites = sitesLanguages.Keys.ToArray();

                // PageType is auto-included and carries the whole schema XML: projecting the page columns avoids
                // repeating it on every row; the (few) page types are loaded once and attached below.
                var pagesGlobalList = await _pageRepository.ToListAsync(
                    WithoutPageType(
                        _pageRepository
                            .Repository
                            .Where(t => allLanguages.Contains(t.FK_LANGUAGE) &&
                                t.ACTIVE == true &&
                                ((t.FK_WEBSITE == null || t.FK_WEBSITE == "") || (t.FK_WEBSITE == website && (t.FK_PRICELIST == null || t.FK_PRICELIST == ""))))));

                var pagesBySiteList = await _pageRepository.ToListAsync(
                    WithoutPageType(
                        _pageRepository
                            .Repository
                            .Where(t => allLanguages.Contains(t.FK_LANGUAGE) &&
                                t.ACTIVE == true &&
                                t.FK_WEBSITE == website &&
                                allSites.Contains(t.FK_PRICELIST))));

                await AttachPageTypes(pagesGlobalList);
                await AttachPageTypes(pagesBySiteList);

                var pagesGlobalDto = _mapper.Map<IList<PageDTO>>(pagesGlobalList).AsReadOnly();
                var pagesBySiteDto = _mapper.Map<IList<PageDTO>>(pagesBySiteList);
                var pagesBySiteLookup = pagesBySiteDto.ToLookup(p => p.Site, StringComparer.OrdinalIgnoreCase);

                var publishablePages = new Dictionary<string, ReadOnlyCollection<PageDTO>>(sitesLanguages.Count);
                foreach (var sl in sitesLanguages)
                {
                    var site = sl.Key;
                    var supportedLanguages = sl.Value;
                    var langSet = supportedLanguages.ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var allpagesBySite = pagesGlobalDto
                            .Where(p => langSet.Contains(p.Language))
                            // pages of a site in a language the site does not publish must not leak into it
                            .Concat(pagesBySiteLookup[site].Where(p => langSet.Contains(p.Language)))
                            .ToList()
                            .AsReadOnly();

                    publishablePages.Add(site, allpagesBySite);
                }

                var result = publishablePages.ToFrozenDictionary(c => c.Key, c => c.Value);

                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<PageDTO>>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUBLISH_PAGES {message}", ex.Message);
                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<PageDTO>>>.MakeFailure([ErrorMessage.Create("PUBLISH_PAGES", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                // the page (and its content) is not loaded just to be deleted
                var deleted = await _pageRepository.ExecuteDeleteAsync(_pageRepository.Repository.Where(p => p.ID == id));

                return deleted > 0
                    ? OperationResult.MakeSuccess()
                    : OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_FOUND")]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REMOVE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "GENERIC_ERROR")]);
            }
        }
    }
}
