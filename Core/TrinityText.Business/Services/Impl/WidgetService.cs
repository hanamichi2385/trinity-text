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
                _logger.LogError("SEARCH", ex);
                return OperationResult<PagedResult<WidgetDTO>>.MakeFailure(new[] { ErrorMessage.Create("SEARCH", "GENERIC_ERROR") });
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
                    return OperationResult<WidgetDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<WidgetDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<WidgetDTO>> GetByKeys(string key, string website, string site, string language)
        {
            try
            {
                var entity = _widgetRepository
                    .Repository
                    .Where(w => w.KEY.Equals(key, StringComparison.InvariantCultureIgnoreCase)
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
                    return OperationResult<WidgetDTO>.MakeFailure(new[] { ErrorMessage.Create("GET_BYKEYS", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<WidgetDTO>.MakeFailure(new[] { ErrorMessage.Create("GET_BYKEYS", "GENERIC_ERROR") });
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
                    return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("REMOVE", ex);
                return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "GENERIC_ERROR") });
            }
        }

        #region Private methods

        private IQueryable<Widget> GetWidgetsByFilter(SearchWidgetDTO search)
        {
            var websites = search.UserWebsites;
            var languages = search.WebsiteLanguages;

            var query =
                _widgetRepository
                .Repository
                .Where(s =>
                    (string.IsNullOrWhiteSpace(s.FK_WEBSITE) ||
                    (!string.IsNullOrWhiteSpace(s.FK_WEBSITE) && websites.Contains(s.FK_WEBSITE, StringComparer.InvariantCultureIgnoreCase)))
                    && languages.Contains(s.FK_LANGUAGE, StringComparer.InvariantCultureIgnoreCase));

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
                        .Where(s => s.KEY.Contains(search.Terms, StringComparison.InvariantCultureIgnoreCase));
                }

                if (search.ShowOnlyDedicated.HasValue)
                {
                    query =
                        query.Where(r => !string.IsNullOrWhiteSpace(r.FK_WEBSITE));
                }

                if ((!search.SortingName.HasValue && !search.SortingWebsite.HasValue && !search.SortingSite.HasValue && !search.SortingLanguage.HasValue) ||
                    (search.SortingName.Value == SortingType.Unordered && search.SortingWebsite.Value == SortingType.Unordered && search.SortingSite.Value == SortingType.Unordered && search.SortingLanguage.Value == SortingType.Unordered))
                {
                    query = query.Sort((r) => r.KEY, SortingType.Ascending);
                }
                else
                {
                    query = query.Sort((r) => r.KEY, search.SortingName);
                    query = query.Sort((r) => r.FK_WEBSITE, search.SortingWebsite);
                    query = query.Sort((r) => r.FK_PRICELIST, search.SortingSite);
                    query = query.Sort((r) => r.FK_LANGUAGE, search.SortingLanguage);
                }
            }

            return query;
        }

        #endregion
    }
}
