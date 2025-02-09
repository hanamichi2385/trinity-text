﻿using AutoMapper;
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
                var query =
                    GetTextsByFilter(search);

                var totalCount = query.Count();

                var list = query
                    .GetPage(page, size)
                    .ToArray();

                var result = new PagedResult<TextDTO>()
                {
                    Page = page,
                    PageSize = size,
                    Result = _mapper.Map<TextDTO[]>(list),
                    TotalCount = totalCount,
                };

                return await Task.FromResult(OperationResult<PagedResult<TextDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SEARCH {message}", ex.Message);
                return OperationResult<PagedResult<TextDTO>>.MakeFailure([ErrorMessage.Create("SEARCH", "GENERIC_ERROR")]);
            }
        }

        private IQueryable<Text> GetTextsByFilter(SearchTextDTO search)
        {
            var websites = search.UserWebsites ?? [];
            var languages = search.WebsiteLanguages ?? [];
            var textTypes = search.TextTypeIds ?? [];

            var query = _textRepository
                .Repository
                .Where(s =>
                        (string.IsNullOrWhiteSpace(s.FK_WEBSITE) ||
                        (!string.IsNullOrWhiteSpace(s.FK_WEBSITE) && websites.Contains(s.FK_WEBSITE)))
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
                        .Where(s => !string.IsNullOrWhiteSpace(s.FK_WEBSITE) == search.ShowOnlyDedicated.Value);
                }

                if (search.ShowOnlyActive.HasValue)
                {
                    query =
                        query.Where(r => r.ACTIVE == search.ShowOnlyActive.Value);
                }

                if ((!search.SortingName.HasValue && !search.SortingWebsite.HasValue && !search.SortingSite.HasValue && !search.SortingLanguage.HasValue) ||
                    (search.SortingName.Value == SortingType.Unordered && search.SortingWebsite.Value == SortingType.Unordered && search.SortingSite.Value == SortingType.Unordered && search.SortingLanguage.Value == SortingType.Unordered))
                {
                    query = query.Sort((r) => r.NAME, SortingType.Ascending);
                }
                else
                {
                    query = query.Sort((r) => r.NAME, search.SortingName);
                    query = query.Sort((r) => r.FK_WEBSITE, search.SortingWebsite);
                    query = query.Sort((r) => r.FK_PRICELIST, search.SortingSite);
                    query = query.Sort((r) => r.FK_LANGUAGE, search.SortingLanguage);
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
                    var result = _mapper.Map<IList<TextRevisionDTO>>(entity.REVISIONS);

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

        private async Task<OperationResult<TextDTO>> Create(TextDTO dto, TextType textType)
        {
            var entity = _mapper.Map<Text>(dto);
            //entity.TEXTTYPE = textType;


            var revision = entity.REVISIONS.ElementAt(0);
            revision.CREATION_DATE = DateTime.Now;
            revision.REVISION_NUMBER = 1;
            entity.ACTIVE = true;
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

                var resx = query.Count();

                return await Task.FromResult(resx == 0 ? OperationResult.MakeSuccess() : OperationResult.MakeFailure([ErrorMessage.Create("DUPLICATED", "DUPLICATED")]));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EXIST {message}", ex.Message);
                return OperationResult<TextDTO>.MakeFailure([ErrorMessage.Create("EXIST", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>> GetPublishableTextsByWebsite(string website, Dictionary<string, string[]> sitesLanguages, IReadOnlyList<TextTypeDTO> textTypes)
        {
            try
            {
                var textTypesIds = textTypes.Select(t => t.Id).Union([null]).ToArray();
                var allLanguages = sitesLanguages.Values.SelectMany(v => v).Distinct().ToArray();

                var textsGlobalByWebsiteList = _textRepository
                    .Repository
                    .Where(t => allLanguages.Contains(t.FK_LANGUAGE) &&
                        textTypesIds.Contains(t.FK_TEXTTYPE) &&
                        t.ACTIVE == true &&
                        (t.FK_WEBSITE == null || (t.FK_WEBSITE == website && string.IsNullOrWhiteSpace(t.FK_PRICELIST))))
                    .ToList();

                var textsGlobalByWebsite = _mapper.Map<IList<TextDTO>>(textsGlobalByWebsiteList).AsReadOnly();

                var publishableTexts = new Dictionary<string, ReadOnlyCollection<TextDTO>>();
                foreach (var sl in sitesLanguages)
                {
                    var site = sl.Key;
                    var supportedLanguages = sl.Value;
                    var textsBySiteList = _textRepository
                        .Repository
                        .Where(t => supportedLanguages.Contains(t.FK_LANGUAGE) &&
                            textTypesIds.Contains(t.FK_TEXTTYPE) &&
                            t.ACTIVE == true &&
                            t.FK_WEBSITE == website && t.FK_PRICELIST == site)
                        .ToList();

                    var textsBySite = _mapper.Map<IList<TextDTO>>(textsBySiteList);

                    var list = new List<TextDTO>();
                    foreach (var l in supportedLanguages)
                    {
                        var textBySiteLang = textsGlobalByWebsite
                            .Where(ttw => ttw.Language == l)
                            .Union(textsBySite.Where(wbs => wbs.Language == l))
                            .OrderBy(l => l.Name)
                            .ToList()
                            .AsReadOnly();

                        var reducedTexts = ReduceTexts(textBySiteLang, website, site, textTypesIds);

                        list.AddRange(reducedTexts);
                    }
                    
                    publishableTexts.Add(site, list.AsReadOnly());
                }

                var rd = publishableTexts.ToFrozenDictionary(p => p.Key, p => p.Value);

                return await Task.FromResult(OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>.MakeSuccess(rd));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUBLISH_TEXTS {message}", ex.Message);
                return OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>.MakeFailure([ErrorMessage.Create("PUBLISH_TEXTS", "GENERIC_ERROR")]);
            }
        }

        private static List<TextDTO> ReduceTexts(IReadOnlyList<TextDTO> texts, string website, string site, int?[] textTypesIds)
        {
            var list = new List<TextDTO>();
            foreach (var t in textTypesIds)
            {
                var listForType = new List<TextDTO>();

                var textsforType =
                    texts
                    .Where(rft => rft.TextType?.Id == t)
                    .ToList()
                    .AsReadOnly();

                foreach (var n in textsforType.Select(s => s.Name).Distinct())
                {
                    var textByName =
                        textsforType
                        .Where(resx => resx.Name.Equals(n, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();

                    if (textByName.Count == 1)
                    {
                        listForType.Add(textByName.First());
                    }
                    else
                    {
                        var textByWebsite =
                            textByName.Where(resx => resx.Website == website)
                            .ToList();

                        if (textByWebsite.Count == 1)
                        {
                            listForType.Add(textByWebsite.First());
                        }
                        else
                        {
                            var textsBySite =
                                textByWebsite.Where(resx => resx.Site == site)
                                .ToList();

                            if (textsBySite.Count == 0)
                            {
                                var textCustomBySite =
                                    textByWebsite.Where(resx => !string.IsNullOrEmpty(resx.Website) && !string.IsNullOrEmpty(resx.Site))
                                    .FirstOrDefault();

                                if (textCustomBySite != null)
                                {
                                    listForType.Add(textCustomBySite);
                                }
                                else
                                {
                                    var globalTexts =
                                        textByName.Where(resx => string.IsNullOrWhiteSpace(resx.Website))
                                        .ToList();

                                    if (globalTexts.Count == 1)
                                    {
                                        listForType.Add(globalTexts.First());
                                    }
                                    else
                                    {
                                        var countries =
                                            globalTexts.Select(ris => ris.Country)
                                            .Distinct()
                                            .ToList();

                                        foreach (var country in countries)
                                        {
                                            var textsForCountry = textByName
                                                .Where(resx => resx.Country == country)
                                                .ToList();

                                            if (textsForCountry.Count == 1)
                                            {
                                                listForType.Add(textsForCountry.First());
                                            }
                                            else
                                            {
                                                var text =
                                                    textsForCountry.Where(ris => ris.Site == site)
                                                    .Single();

                                                listForType.Add(text);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (textsBySite.Count == 1)
                                {
                                    listForType.Add(textsBySite.First());
                                }
                                else
                                {

                                    var countries =
                                        textByName.Select(ris => ris.Country)
                                        .Distinct()
                                        .ToList();

                                    foreach (var country in countries)
                                    {
                                        var textsForCountries = textByName
                                            .Where(resx => resx.Country == country)
                                            .ToList();

                                        if (textsForCountries.Count == 1)
                                        {
                                            listForType.Add(textsForCountries.First());
                                        }
                                        else
                                        {
                                            var text =
                                                textsForCountries.Where(ris => ris.Site == site)
                                                .Single();

                                            listForType.Add(text);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                list.AddRange(listForType);
            }
            return list;
        }

        //TODO: sistemare
        public async Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>> GetPublishableTexts(string website, string site, string[] languages, IReadOnlyList<TextTypeDTO> textTypes)
        {
            try
            {
                var publishableTexts = new Dictionary<string, List<TextDTO>>();

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

                var query =
                    GetTextsByFilter(search);

                var q = query.ToList();

                var all = _mapper.Map<IList<TextDTO>>(q).AsReadOnly();

                foreach (var l in languages)
                {
                    var texts =
                        all
                        .Where(n => n.Language == l)
                        .OrderBy(n => n.Name)
                        .ToList();

                    //var types = texts
                    //    .Select(t => t.TextType)
                    //    .Distinct()
                    //    .ToList();

                    var list = new List<TextDTO>();
                    foreach (var t in textTypesIds)
                    {
                        var listForType = new List<TextDTO>();

                        var textsforType =
                            texts
                            .Where(rft => rft.TextType?.Id == t)
                            .ToList()
                            .AsReadOnly();

                        foreach (var n in textsforType.Select(s => s.Name).Distinct())
                        {
                            var textByName =
                                textsforType
                                .Where(resx => resx.Name.Equals(n, StringComparison.InvariantCultureIgnoreCase))
                                .ToList();

                            if (textByName.Count == 1)
                            {
                                listForType.Add(textByName.First());
                            }
                            else
                            {
                                var textByWebsite =
                                    textByName.Where(resx => resx.Website == website)
                                    .ToList();

                                var globalTexts =
                                    textByName.Where(resx => string.IsNullOrWhiteSpace(resx.Website))
                                    .ToList();

                                if (textByWebsite.Count == 1)
                                {
                                    listForType.Add(textByWebsite.First());
                                }
                                else
                                {
                                    var textsBySite =
                                        textByWebsite.Where(resx => resx.Site == site)
                                        .ToList();

                                    if (textsBySite.Count == 0)
                                    {
                                        var textCustomBySite =
                                            textByWebsite.Where(resx => !string.IsNullOrEmpty(resx.Website) && !string.IsNullOrEmpty(resx.Site))
                                            .FirstOrDefault();

                                        if (textCustomBySite != null)
                                        {
                                            listForType.Add(textCustomBySite);
                                        }
                                        else
                                        {
                                            if (globalTexts.Count == 1)
                                            {
                                                listForType.Add(globalTexts.First());
                                            }
                                            else
                                            {
                                                var countries =
                                                    globalTexts.Select(ris => ris.Country)
                                                    .Distinct()
                                                    .ToList();

                                                foreach (var country in countries)
                                                {
                                                    var textsForCountry = textByName
                                                        .Where(resx => resx.Country == country)
                                                        .ToList();

                                                    if (textsForCountry.Count == 1)
                                                    {
                                                        listForType.Add(textsForCountry.First());
                                                    }
                                                    else
                                                    {
                                                        var text =
                                                            textsForCountry.Where(ris => ris.Site == site)
                                                            .Single();

                                                        listForType.Add(text);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (textsBySite.Count == 1)
                                        {
                                            listForType.Add(textsBySite.First());
                                        }
                                        else
                                        {

                                            var countries =
                                                textByName.Select(ris => ris.Country)
                                                .Distinct()
                                                .ToList();

                                            foreach (var country in countries)
                                            {
                                                var textsForCountries = textByName
                                                    .Where(resx => resx.Country == country)
                                                    .ToList();

                                                if (textsForCountries.Count == 1)
                                                {
                                                    listForType.Add(textsForCountries.First());
                                                }
                                                else
                                                {
                                                    var text =
                                                        textsForCountries.Where(ris => ris.Site == site)
                                                        .Single();

                                                    listForType.Add(text);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        list.AddRange(listForType);
                    }

                    publishableTexts.Add(l, list);
                }

                var rd = publishableTexts.ToFrozenDictionary(p => p.Key, p => p.Value.AsReadOnly());

                return await Task.FromResult(OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>.MakeSuccess(rd));
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
                var entity = await _textRepository
                    .Read(id);

                if (entity != null)
                {
                    var i = entity.REVISIONS.Count;
                    while (i > 0)
                    {
                        var t = entity.REVISIONS.ElementAt(i - 1);
                        await _textRevisionRepository.Delete(t);

                        i--;
                    }


                    await _textRepository.Delete(entity);

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

        public async Task<OperationResult> CleanRevisions(int revisionToMantain)
        {
            try
            {
                var texts =
                    _textRepository
                        .Repository
                        .Where(r => r.REVISIONS.Count > revisionToMantain)
                        .ToList();

                foreach (var t in texts)
                {
                    var revisions =
                        t.REVISIONS
                        .OrderByDescending(s => s.CREATION_DATE)
                        .Skip(revisionToMantain)
                        .ToList();

                    foreach (var rev in revisions)
                    {
                        await _textRevisionRepository.Delete(rev);
                    }
                }
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
                foreach (var r in texts)
                {
                    var t = _textRepository
                        .Repository
                        .Where(rx =>
                            rx.FK_TEXTTYPE == typeId
                            && rx.NAME == r.Name
                            && rx.FK_LANGUAGE == r.Language
                            && rx.FK_WEBSITE == r.Website
                            && rx.FK_PRICELIST == r.Site
                            && rx.FK_COUNTRY == r.Country
                        )
                        .FirstOrDefault();

                    var exist = t != null;

                    if ((exist == false) || (exist == true && @override))
                    {
                        if (exist)
                        {
                            var updateRs = await Update(r, t, textType);
                            if (updateRs.Success)
                            {
                                counter++;
                            }
                        }
                        else
                        {
                            var saveRs = await Create(r, textType);
                            if (saveRs.Success)
                            {
                                counter++;
                            }
                        }
                    }
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
