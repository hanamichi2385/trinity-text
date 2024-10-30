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
    public class WidgetService : IWidgetService
    {
        private readonly IRepository<Widget> _widgetRepository;

        private readonly ILogger<WidgetService> _logger;

        private readonly IMapper _mapper;

        public WidgetService(IRepository<Widget> pageTypeRepository, IMapper mapper, ILogger<WidgetService> logger)
        {
            _widgetRepository = pageTypeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<PagedResult<WidgetDTO>>> Search(SearchWidgetDTO search, int page, int size)
        {
            try
            {
                var query =
                    GetWidgetsByFilter(search);

                var totalCount = query.Count();

                var list = query
                    .GetPage(page, size)
                    .ToList();

                var result = new PagedResult<WidgetDTO>()
                {
                    Page = page,
                    PageSize = size,
                    Result = _mapper.Map<IList<WidgetDTO>>(list),
                    TotalCount = totalCount,
                };

                return await Task.FromResult(OperationResult<PagedResult<WidgetDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SEARCH {message}", ex.Message);
                return OperationResult<PagedResult<WidgetDTO>>.MakeFailure([ErrorMessage.Create("SEARCH", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<WidgetDTO>> Get(int id)
        {
            try
            {
                var entity = await _widgetRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = _mapper.Map<WidgetDTO>(entity);

                    return OperationResult<WidgetDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<WidgetDTO>> GetByKeys(string key, string website, string site, string language)
        {
            try
            {
                var entity = _widgetRepository
                    .Repository
                    .Where(w => w.KEY.Equals(key)
                        && w.FK_LANGUAGE == language &&
                        (
                            (string.IsNullOrWhiteSpace(w.FK_PRICELIST) || w.FK_PRICELIST == site)
                            &&
                            (string.IsNullOrWhiteSpace(w.FK_WEBSITE) || w.FK_WEBSITE == website)
                        )
                    )
                    .OrderByDescending(w => w.FK_PRICELIST)
                    .ThenByDescending(w => w.FK_WEBSITE)
                    .FirstOrDefault();

                if (entity != null)
                {
                    var result = _mapper.Map<WidgetDTO>(entity);

                    return await Task.FromResult(OperationResult<WidgetDTO>.MakeSuccess(result));
                }
                else
                {
                    return OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("GET_BYKEYS", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET_BYKEYS {message}", ex.Message);
                return OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("GET_BYKEYS", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _widgetRepository
                    .Read(id);

                if (entity != null)
                {
                    await _widgetRepository.Delete(entity);

                    return OperationResult.MakeSuccess();
                }
                else
                {
                    return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REMOVE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<WidgetDTO>> Save(WidgetDTO dto)
        {
            try
            {
                if (dto.Id.HasValue)
                {
                    var entity = await _widgetRepository
                        .Read(dto.Id.Value);
                    if (entity != null)
                    {
                        //entity.ACTIVE = dto.Active;
                        entity.FK_LANGUAGE = dto.Language;
                        entity.FK_PRICELIST = dto.Site;
                        entity.FK_WEBSITE = dto.Website;
                        entity.KEY = dto.Key;
                        entity.LASTUPDATE_DATE = DateTime.Now;
                        entity.CONTENT = dto.Content;

                        var result = await _widgetRepository.Update(entity);

                        var r = _mapper.Map<WidgetDTO>(result);
                        return OperationResult<WidgetDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                    }
                }
                else
                {
                    var existRs = await NotDuplicated(dto);

                    if (existRs.Success)
                    {
                        var entity = _mapper.Map<Widget>(dto);
                        entity.LASTUPDATE_DATE = DateTime.Now;
                        entity.CREATION_DATE = DateTime.Now;
                        await _widgetRepository.Create(entity);

                        var r = _mapper.Map<WidgetDTO>(entity);

                        return OperationResult<WidgetDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<WidgetDTO>.MakeFailure(existRs.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }

        #region Private methods

        private async Task<OperationResult> NotDuplicated(WidgetDTO dto)
        {
            try
            {
                var query =
                     _widgetRepository
                     .Repository
                     .Where(w => w.KEY == dto.Key && w.FK_LANGUAGE == dto.Language
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

                return await Task.FromResult(resx == 0 ? OperationResult.MakeSuccess() : OperationResult.MakeFailure([ErrorMessage.Create("DUPLICATED", "DUPLICATED")]));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EXIST {message}", ex.Message);
                return OperationResult<TextDTO>.MakeFailure([ErrorMessage.Create("EXIST", "GENERIC_ERROR")]);
            }
        }

        private IQueryable<Widget> GetWidgetsByFilter(SearchWidgetDTO search)
        {
            var websites = search.UserWebsites ?? [];
            var languages = search.WebsiteLanguages ?? [];

            var query =
                _widgetRepository
                .Repository
                .Where(s =>
                    (string.IsNullOrWhiteSpace(s.FK_WEBSITE) ||
                    (!string.IsNullOrWhiteSpace(s.FK_WEBSITE) && websites.Contains(s.FK_WEBSITE)))
                    && languages.Contains(s.FK_LANGUAGE));

            if (search != null)
            {
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

                if (!string.IsNullOrWhiteSpace(search.LanguageId))
                {
                    query =
                        query
                        .Where(s =>
                        s.FK_LANGUAGE == search.LanguageId);
                }

                if (!string.IsNullOrWhiteSpace(search.Terms))
                {
                    query =
                        query
                        .Where(s => s.KEY.Contains(search.Terms));
                }

                if (search.ShowOnlyDedicated.HasValue)
                {
                    query =
                        query.Where(r => !string.IsNullOrWhiteSpace(r.FK_WEBSITE));
                }

                if ((!search.SortingName.HasValue && !search.SortingWebsite.HasValue && !search.SortingSite.HasValue && !search.SortingLanguage.HasValue && !search.SortingLastUpdate.HasValue) ||
                    (search.SortingName.Value == SortingType.Unordered && search.SortingWebsite.Value == SortingType.Unordered && search.SortingSite.Value == SortingType.Unordered && search.SortingLanguage.Value == SortingType.Unordered && search.SortingLastUpdate.Value == SortingType.Unordered))
                {
                    query = query.Sort((r) => r.KEY, SortingType.Ascending);
                }
                else
                {
                    query = query.Sort((r) => r.KEY, search.SortingName);
                    query = query.Sort((r) => r.FK_WEBSITE, search.SortingWebsite);
                    query = query.Sort((r) => r.FK_PRICELIST, search.SortingSite);
                    query = query.Sort((r) => r.FK_LANGUAGE, search.SortingLanguage);
                    query = query.Sort((r) => r.LASTUPDATE_DATE, search.SortingLastUpdate);
                }
            }

            return query;
        }

        #endregion
    }
}
