using AutoMapper;
using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.Business.Services.Impl
{
    public class TextService : ITextService
    {
        private readonly IRepository<Text> _textRepository;

        private readonly IRepository<TextRevision> _textRevisionRepository;

        private readonly IRepository<TextType> _textTypeRevisionRepository;

        private readonly ILogger<TextService> _logger;

        private readonly IMapper _mapper;

        public TextService(IRepository<Text> textRepository, IRepository<TextRevision> textRevisionRepository, IRepository<TextType> textTypeRevisionRepository, IMapper mapper, ILogger<TextService> logger)
        {
            _textRepository = textRepository;
            _textRevisionRepository = textRevisionRepository;
            _textTypeRevisionRepository = textTypeRevisionRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<PagedResult<TextDTO>>> Search(SearchTextDTO search, int page, int size)
        {
            try
            {
                var query = GetTextsByFilter(search);

                var totalCount = await _textRepository.CountAsync(query);
                var list = await _textRepository.ToListAsync(query.GetPage(page, size));

                await PopulateLatestRevisions(list);

                var result = new PagedResult<TextDTO>()
                {
                    Page = page,
                    PageSize = size,
                    Result = _mapper.Map<TextDTO[]>(list),
                    TotalCount = totalCount,
                };

                return OperationResult<PagedResult<TextDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SEARCH {message}", ex.Message);
                return OperationResult<PagedResult<TextDTO>>.MakeFailure([ErrorMessage.Create("SEARCH", "GENERIC_ERROR")]);
            }
        }

        private IQueryable<Text> GetTextsByFilter(SearchTextDTO search, bool applySorting = true)
        {
            var websites = search.UserWebsites ?? [];
            var languages = search.WebsiteLanguages ?? [];
            var textTypes = search.TextTypeIds ?? [];

            var query = _textRepository
                .Repository
                .Where(s =>
                        ((s.FK_WEBSITE == null || s.FK_WEBSITE == "") ||
                        ((s.FK_WEBSITE != null && s.FK_WEBSITE != "") && websites.Contains(s.FK_WEBSITE)))
                        && languages.Contains(s.FK_LANGUAGE));

            if (search != null)
            {
                if (textTypes.Length > 0)
                {
                    query =
                        query
                        .Where(s => textTypes.Contains(s.FK_TEXTTYPE));
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
                        .Where(s => s.NAME.Contains(search.Terms));
                }

                if (search.ShowOnlyDedicated.HasValue)
                {
                    query =
                        query
                        .Where(s => (s.FK_WEBSITE != null && s.FK_WEBSITE != "") == search.ShowOnlyDedicated.Value);
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

                    if (sortName == SortingType.Unordered && sortWebsite == SortingType.Unordered && sortSite == SortingType.Unordered && sortLanguage == SortingType.Unordered)
                    {
                        query = query.Sort((r) => r.NAME, SortingType.Ascending);
                    }
                    else
                    {
                        query = query.Sort((r) => r.NAME, sortName);
                        query = query.Sort((r) => r.FK_WEBSITE, sortWebsite);
                        query = query.Sort((r) => r.FK_PRICELIST, sortSite);
                        query = query.Sort((r) => r.FK_LANGUAGE, sortLanguage);
                    }
                }
            }

            return query;
        }

        public async Task<OperationResult<IList<TextRevisionDTO>>> GetAllRevisions(int textId)
        {
            try
            {
                var entity = await _textRepository
                    .Read(textId);

                if (entity != null)
                {
                    var revisions = await _textRevisionRepository.ToListAsync(
                        _textRevisionRepository
                            .Repository
                            .Where(r => r.FK_TEXT == textId));

                    var result = _mapper.Map<IList<TextRevisionDTO>>(revisions);

                    return OperationResult<IList<TextRevisionDTO>>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<IList<TextRevisionDTO>>.MakeFailure([ErrorMessage.Create("GETALLREVISIONS", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALLREVISIONS {message}", ex.Message);
                return OperationResult<IList<TextRevisionDTO>>.MakeFailure([ErrorMessage.Create("GETALLREVISIONS", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<TextDTO>> Get(int id)
        {
            try
            {
                var entity = await _textRepository
                    .Read(id);

                if (entity != null)
                {
                    await PopulateLatestRevisions([entity]);

                    var result = _mapper.Map<TextDTO>(entity);

                    return OperationResult<TextDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<TextDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<TextDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<TextDTO>> Save(TextDTO dto)
        {
            try
            {
                var textType = default(TextType);
                if (dto.TextTypeId.HasValue)
                {
                    textType = await _textTypeRevisionRepository.Read(dto.TextTypeId.Value);
                }

                var existRs = await NotDuplicated(dto);
                if (existRs.Success)
                {
                    if (dto.Id.HasValue)
                    {
                        var entity = await _textRepository.Read(dto.Id.Value);
                        if (entity != null)
                        {
                            await PopulateLatestRevisions([entity]);
                        }
                        return await Update(dto, entity, textType);
                    }
                    else
                    {
                        return await Create(dto, textType);
                    }

                }
                else
                {
                    return OperationResult<TextDTO>.MakeFailure(existRs.Errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<TextDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }

        private static (string, string, string, string, string) ImportKey(string name, string language, string website, string site, string country)
            => (name?.ToUpperInvariant(), language?.ToUpperInvariant(), website?.ToUpperInvariant(), site?.ToUpperInvariant(), country?.ToUpperInvariant());

        /// <summary>
        /// Loads only the most recent revision (by REVISION_NUMBER) for each given Text in a single query and
        /// assigns it to <see cref="Text.REVISIONS"/>. Replaces the former AutoInclude that eagerly loaded every
        /// historical revision (with full CONTENT) on every Text read.
        /// </summary>
        private async Task PopulateLatestRevisions(IEnumerable<Text> texts)
        {
            var list = texts as ICollection<Text> ?? texts.ToList();
            var ids = list.Select(t => t.ID).Distinct().ToArray();
            if (ids.Length == 0)
            {
                return;
            }

            var latest = await _textRevisionRepository.ToListAsync(
                _textRevisionRepository
                    .Repository
                    .Where(r => ids.Contains(r.FK_TEXT)
                        && r.REVISION_NUMBER == _textRevisionRepository
                            .Repository
                            .Where(x => x.FK_TEXT == r.FK_TEXT)
                            .Max(x => x.REVISION_NUMBER)));

            var byText = latest.ToLookup(r => r.FK_TEXT);
            foreach (var t in list)
            {
                t.REVISIONS = byText[t.ID].ToList();
            }
        }

        private Text BuildNewText(TextDTO dto)
        {
            var entity = _mapper.Map<Text>(dto);
            //entity.TEXTTYPE = textType;


            var revision = entity.REVISIONS.ElementAt(0);
            revision.CREATION_DATE = DateTime.Now;
            revision.REVISION_NUMBER = 1;
            entity.ACTIVE = true;

            return entity;
        }

        private async Task<OperationResult<TextDTO>> Create(TextDTO dto, TextType textType)
        {
            var entity = BuildNewText(dto);
            await _textRepository.Create(entity);

            var r = _mapper.Map<TextDTO>(entity);
            r.TextType = _mapper.Map<TextTypeDTO>(textType);

            return OperationResult<TextDTO>.MakeSuccess(r);
        }

        private async Task<OperationResult<TextDTO>> Update(TextDTO dto, Text entity, TextType textType)
        {
            if (entity != null)
            {
                //entity.FK_TEXTTYPE = dto.TextTypeId;
                entity.ACTIVE = dto.Active;
                entity.FK_COUNTRY = dto.Country;
                entity.FK_LANGUAGE = dto.Language;
                entity.FK_PRICELIST = dto.Site;
                //entity.FK_TEXTTYPE = dto.TextTypeId;
                entity.FK_WEBSITE = dto.Website;
                entity.NAME = dto.Name;

                if (entity.FK_TEXTTYPE != dto.TextTypeId)
                {
                    if (dto.TextTypeId.HasValue)
                    {
                        entity.TEXTTYPE = textType;
                    }
                    else
                    {
                        entity.TEXTTYPE = null;
                        entity.FK_TEXTTYPE = null;
                    }
                }

                var lastRevision = entity.REVISIONS.OrderByDescending(d => d.CREATION_DATE).FirstOrDefault();
                if (lastRevision != null && string.Equals(lastRevision.CONTENT, dto.TextRevision.Content) == false)
                {
                    var revision = _mapper.Map<TextRevision>(dto.TextRevision);
                    //revision.TEXT = entity;
                    //revision.FK_TEXT = entity.ID;
                    revision.REVISION_NUMBER = lastRevision.REVISION_NUMBER + 1;
                    revision.CREATION_DATE = DateTime.Now;

                    entity.REVISIONS.Add(revision);

                    //await _textRevisionRepository.Create(revision);
                }

                var result = await _textRepository.Update(entity);

                var r = _mapper.Map<TextDTO>(result);
                return OperationResult<TextDTO>.MakeSuccess(r);
            }
            else
            {
                return OperationResult<TextDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
            }
        }

        private async Task<OperationResult> NotDuplicated(TextDTO dto)
        {
            try
            {
                var query =
                    _textRepository.Repository
                        .Where(r => r.NAME == dto.Name
                            && r.FK_LANGUAGE == dto.Language);

                if (dto.TextTypeId.HasValue)
                {
                    query =
                        query.Where(r => r.FK_TEXTTYPE == dto.TextTypeId.Value);
                }
                else
                {
                    query =
                        query.Where(r => r.FK_TEXTTYPE == null);
                }

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

                if (!string.IsNullOrWhiteSpace(dto.Country))
                {
                    query =
                        query.Where(r => r.FK_COUNTRY == dto.Country);
                }
                else
                {
                    query =
                        query.Where(r => r.FK_COUNTRY == null);
                }

                if (dto.Id.HasValue)
                {
                    query = query.Where(r => r.ID != dto.Id.Value);
                }

                var resx = await _textRepository.CountAsync(query);

                return resx == 0 ? OperationResult.MakeSuccess() : OperationResult.MakeFailure([ErrorMessage.Create("DUPLICATED", "DUPLICATED")]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EXIST {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("EXIST", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>> GetPublishableTextsByWebsite(string website, Dictionary<string, string[]> sitesLanguages, IReadOnlyList<TextTypeDTO> textTypes)
        {
            try
            {
                var textTypesIds = textTypes.Select(t => t.Id).Union([null]).ToArray();
                var allLanguages = sitesLanguages.Values.SelectMany(v => v).Distinct().ToArray();
                var allSites = sitesLanguages.Keys.ToArray();

                var textsGlobalByWebsiteList = await _textRepository.ToListAsync(
                    _textRepository
                        .Repository
                        .Where(t => allLanguages.Contains(t.FK_LANGUAGE) &&
                            textTypesIds.Contains(t.FK_TEXTTYPE) &&
                            t.ACTIVE == true &&
                            (t.FK_WEBSITE == null || (t.FK_WEBSITE == website && (t.FK_PRICELIST == null || t.FK_PRICELIST == "")))));

                await PopulateLatestRevisions(textsGlobalByWebsiteList);
                var textsGlobalByWebsite = _mapper.Map<IList<TextDTO>>(textsGlobalByWebsiteList).AsReadOnly();
                // grouped once: the loop below runs for every site x language
                var textsGlobalByLanguage = textsGlobalByWebsite.ToLookup(t => t.Language);

                var textsBySiteList = await _textRepository.ToListAsync(
                    _textRepository
                        .Repository
                        .Where(t => allLanguages.Contains(t.FK_LANGUAGE) &&
                            textTypesIds.Contains(t.FK_TEXTTYPE) &&
                            t.ACTIVE == true &&
                            t.FK_WEBSITE == website &&
                            allSites.Contains(t.FK_PRICELIST)));

                await PopulateLatestRevisions(textsBySiteList);
                var textsBySiteAll = _mapper.Map<IList<TextDTO>>(textsBySiteList);
                var textsBySiteLookup = textsBySiteAll.ToLookup(t => t.Site, StringComparer.OrdinalIgnoreCase);

                var publishableTexts = new Dictionary<string, ReadOnlyCollection<TextDTO>>(sitesLanguages.Count);
                foreach (var sl in sitesLanguages)
                {
                    var site = sl.Key;
                    var supportedLanguages = sl.Value;
                    var textsBySite = textsBySiteLookup[site].ToList();

                    var list = new List<TextDTO>();
                    foreach (var l in supportedLanguages)
                    {
                        // global and site-specific texts are disjoint sets: Concat is enough (no hashing like Union)
                        var textBySiteLang = textsGlobalByLanguage[l]
                            .Concat(textsBySite.Where(wbs => wbs.Language == l))
                            .OrderBy(x => x.Name)
                            .ToList()
                            .AsReadOnly();

                        var reducedTexts = ReduceTexts(textBySiteLang, website, site, textTypesIds);

                        list.AddRange(reducedTexts);
                    }

                    publishableTexts.Add(site, list.AsReadOnly());
                }

                var rd = publishableTexts.ToFrozenDictionary(p => p.Key, p => p.Value);

                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>.MakeSuccess(rd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUBLISH_TEXTS {message}", ex.Message);
                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>.MakeFailure([ErrorMessage.Create("PUBLISH_TEXTS", "GENERIC_ERROR")]);
            }
        }

        private static List<TextDTO> ReduceTexts(IReadOnlyList<TextDTO> texts, string website, string site, int?[] textTypesIds)
        {
            var byType = texts.ToLookup(t => t.TextType?.Id);
            var list = new List<TextDTO>(texts.Count);
            foreach (var t in textTypesIds)
            {
                var textsforType = byType[t].ToList();
                if (textsforType.Count == 0)
                {
                    continue;
                }

                var byName = textsforType.ToLookup(s => s.Name, StringComparer.OrdinalIgnoreCase);
                foreach (var grouping in byName)
                {
                    var textByName = grouping.ToList();
                    PickBest(textByName, website, site, list);
                }
            }
            return list;
        }

        private static void PickBest(List<TextDTO> textByName, string website, string site, List<TextDTO> output)
        {
            if (textByName.Count == 1)
            {
                output.Add(textByName[0]);
                return;
            }

            var textByWebsite = textByName.Where(resx => resx.Website == website).ToList();

            if (textByWebsite.Count == 1)
            {
                output.Add(textByWebsite[0]);
                return;
            }

            var textsBySite = textByWebsite.Where(resx => resx.Site == site).ToList();

            if (textsBySite.Count == 0)
            {
                var textCustomBySite = textByWebsite
                    .FirstOrDefault(resx => !string.IsNullOrEmpty(resx.Website) && !string.IsNullOrEmpty(resx.Site));

                if (textCustomBySite != null)
                {
                    output.Add(textCustomBySite);
                    return;
                }

                var globalTexts = textByName.Where(resx => string.IsNullOrWhiteSpace(resx.Website)).ToList();

                if (globalTexts.Count == 1)
                {
                    output.Add(globalTexts[0]);
                    return;
                }

                var countries = globalTexts.Select(ris => ris.Country).Distinct().ToList();
                AppendByCountry(textByName, countries, site, output);
                return;
            }

            if (textsBySite.Count == 1)
            {
                output.Add(textsBySite[0]);
                return;
            }

            var countriesAll = textByName.Select(ris => ris.Country).Distinct().ToList();
            AppendByCountry(textByName, countriesAll, site, output);
        }

        private static void AppendByCountry(List<TextDTO> textByName, List<string> countries, string site, List<TextDTO> output)
        {
            foreach (var country in countries)
            {
                var textsForCountry = textByName.Where(resx => resx.Country == country).ToList();

                if (textsForCountry.Count == 1)
                {
                    output.Add(textsForCountry[0]);
                }
                else
                {
                    var text = textsForCountry.Single(ris => ris.Site == site);
                    output.Add(text);
                }
            }
        }

        public async Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>> GetPublishableTexts(string website, string site, string[] languages, IReadOnlyList<TextTypeDTO> textTypes)
        {
            try
            {
                var publishableTexts = new Dictionary<string, List<TextDTO>>(languages.Length);

                var textTypesIds = textTypes.Select(t => t.Id).Union([null]).ToArray();

                var search = new SearchTextDTO()
                {
                    Website = website,
                    Site = site,
                    LanguageIds = languages,
                    ShowOnlyActive = true,
                    UserWebsites = [website],
                    WebsiteLanguages = languages,
                    TextTypeIds = textTypesIds,
                };

                // rows are ordered by name per language in memory below: skip the SQL ORDER BY
                var query = GetTextsByFilter(search, applySorting: false);
                var q = await _textRepository.ToListAsync(query);
                await PopulateLatestRevisions(q);
                var all = _mapper.Map<IList<TextDTO>>(q).AsReadOnly();

                var byLanguage = all.ToLookup(n => n.Language);

                foreach (var l in languages)
                {
                    var texts = byLanguage[l]
                        .OrderBy(n => n.Name)
                        .ToList();

                    publishableTexts.Add(l, ReduceTexts(texts, website, site, textTypesIds));
                }

                var rd = publishableTexts.ToFrozenDictionary(p => p.Key, p => p.Value.AsReadOnly());

                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>.MakeSuccess(rd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUBLISH_TEXTS {message}", ex.Message);
                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>.MakeFailure([ErrorMessage.Create("PUBLISH_TEXTS", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                await _textRepository.BeginTransaction();

                await _textRepository.ExecuteDeleteAsync(
                    _textRevisionRepository
                        .Repository
                        .Where(r => r.FK_TEXT == id));

                var deleted = await _textRepository.ExecuteDeleteAsync(
                    _textRepository
                        .Repository
                        .Where(t => t.ID == id));

                if (deleted == 0)
                {
                    await _textRepository.RollbackTransaction();
                    return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_FOUND")]);
                }

                await _textRepository.CommitTransaction();
                return OperationResult.MakeSuccess();
            }
            catch (Exception ex)
            {
                await _textRepository.RollbackTransaction();
                _logger.LogError(ex, "REMOVE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> CleanRevisions(int revisionToMantain)
        {
            try
            {
                // A revision is "in excess" (to be deleted) when at least `revisionToMantain` newer revisions
                // exist for the same text — i.e. it falls outside the most-recent N. Computed in SQL via a
                // correlated count, so no revision content is loaded into memory.
                // Single DELETE with the same predicate: no id list is materialised and sent back as a huge IN (...).
                await _textRevisionRepository.ExecuteDeleteAsync(
                    _textRevisionRepository
                        .Repository
                        .Where(r => _textRevisionRepository
                            .Repository
                            .Count(x => x.FK_TEXT == r.FK_TEXT && x.CREATION_DATE > r.CREATION_DATE) >= revisionToMantain));

                return OperationResult.MakeSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CLEAN_REVISIONS {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("CLEAN_REVISIONS", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<int>> ImportTexts(TextTypeDTO type, IList<TextDTO> texts, bool @override)
        {
            try
            {
                var counter = 0;
                await _textRepository.BeginTransaction();

                var typeId = type?.Id;
                var textType = default(TextType);
                if (type != null && type.Id.HasValue)
                {
                    textType = await _textTypeRevisionRepository.Read(type.Id.Value);
                }

                var names = texts.Select(t => t.Name).Distinct().ToArray();
                var languages = texts.Select(t => t.Language).Distinct().ToArray();

                var existing = await _textRepository.ToListAsync(
                    _textRepository
                        .Repository
                        .Where(x =>
                            x.FK_TEXTTYPE == typeId
                            && names.Contains(x.NAME)
                            && languages.Contains(x.FK_LANGUAGE)));

                // The @override path calls Update, which compares against the latest revision's content;
                // populate it explicitly (REVISIONS is no longer auto-included).
                await PopulateLatestRevisions(existing);

                // Key components normalized to upper case: the original per-row DB lookup relied on the
                // case-insensitive SQL collation (and NAME is stored upper-cased by the mapper).
                var existingMap = new Dictionary<(string, string, string, string, string), Text>(existing.Count);
                foreach (var e in existing)
                {
                    existingMap[ImportKey(e.NAME, e.FK_LANGUAGE, e.FK_WEBSITE, e.FK_PRICELIST, e.FK_COUNTRY)] = e;
                }

                // new texts are inserted in a single batch (one SaveChanges / flush) instead of one per row
                var toCreate = new List<Text>();
                foreach (var r in texts)
                {
                    existingMap.TryGetValue(ImportKey(r.Name, r.Language, r.Website, r.Site, r.Country), out var t);
                    var exist = t != null;

                    if (!exist)
                    {
                        toCreate.Add(BuildNewText(r));
                    }
                    else if (@override)
                    {
                        var rs = await Update(r, t, textType);
                        if (rs.Success)
                        {
                            counter++;
                        }
                    }
                }

                if (toCreate.Count > 0)
                {
                    await _textRepository.AddRangeAsync(toCreate);
                    counter += toCreate.Count;
                }
                await _textRepository.CommitTransaction();

                return OperationResult<int>.MakeSuccess(counter);
            }
            catch (Exception ex)
            {
                await _textRepository.RollbackTransaction();
                _logger.LogError(ex, "IMPORT_TEXTS {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("IMPORT_TEXTS", "GENERIC_ERROR")]);
            }
        }



        //public int? GetIdByAttributes(string nome, string idVendor, string idIstanza, int? idTipologia, string idLingua, string idNazione)
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        IQueryable<Resource> resource = context.Risorse
        //            .Where(r => r.NOME == nome);

        //        if (!string.IsNullOrEmpty(idLingua))
        //        {
        //            resource = resource.Where(r => r.FK_LINGUA == idLingua);
        //        }

        //        if (!string.IsNullOrEmpty(idNazione))
        //        {
        //            resource = resource.Where(r => r.FK_LINGUA == idNazione);
        //        }

        //        if (!string.IsNullOrEmpty(idVendor))
        //        {
        //            resource = resource.Where(r => r.FK_VENDOR == idVendor);
        //        }

        //        if (!string.IsNullOrEmpty(idIstanza))
        //        {
        //            resource = resource.Where(r => r.FK_ISTANZA == idIstanza);
        //        }

        //        if (idTipologia.HasValue)
        //        {
        //            resource = resource.Where(r => r.FK_TIPOLOGIA == idTipologia.Value);
        //        }

        //        var resourceId =
        //            resource.Select(r => r.ID).First();

        //        return resourceId;
        //    }
        //}
        //public int GetRevisionsCount()
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        var count =
        //            context.TestiPerRisorsa
        //            .Count();

        //        return count;
        //    }
        //}

        //public int GetRevisionsErasable(int number)
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        var count =
        //            context.Risorse
        //            .Where(r => r.REVISIONI.Count > number)
        //            .Count();

        //        return count;
        //    }
        //}
    }
}
