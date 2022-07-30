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
    public class TextService : ITextService
    {
        private IRepository<Text> _textRepository;

        private IRepository<TextRevision> _textRevisionRepository;

        private ILogger<TextService> _logger;

        private IMapper _mapper;

        public TextService(IRepository<Text> textRepository, IRepository<TextRevision> textRevisionRepository, IMapper mapper, ILogger<TextService> logger)
        {
            _textRepository = textRepository;
            _textRevisionRepository = textRevisionRepository;
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
                    .ToList();

                var result = new PagedResult<TextDTO>()
                {
                    Page = page,
                    PageSize = size,
                    Result = _mapper.Map<IList<TextDTO>>(list),
                    TotalCount = totalCount,
                };

                return await Task.FromResult(OperationResult<PagedResult<TextDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("SEARCH", ex);
                return OperationResult<PagedResult<TextDTO>>.MakeFailure(new[] { ErrorMessage.Create("SEARCH", "GENERIC_ERROR") });
            }
        }

        private IQueryable<Text> GetTextsByFilter(SearchTextDTO search)
        {
            var websites = search.UserWebsites;
            var languages = search.UserWebsites;

            var query =
                _textRepository
                .Repository
                .Where(s =>
                    (string.IsNullOrWhiteSpace(s.FK_WEBSITE) ||
                    (!string.IsNullOrWhiteSpace(s.FK_WEBSITE) && websites.Contains(s.FK_WEBSITE, StringComparer.InvariantCultureIgnoreCase)))
                    && languages.Contains(s.FK_LANGUAGE, StringComparer.InvariantCultureIgnoreCase));

            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Terms))
                {
                    query =
                        query
                        .Where(s => s.NAME.Contains(search.Terms, StringComparison.InvariantCultureIgnoreCase));
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
                        (string.IsNullOrEmpty(s.FK_PRICELIST) ||
                        (!string.IsNullOrEmpty(s.FK_PRICELIST) && s.FK_PRICELIST == search.Site)));
                }

                if (search.TextTypeId.HasValue)
                {
                    var typeId = search.TextTypeId.Value;
                    if (typeId != -1)
                    {
                        query =
                            query
                            .Where(s => s.FK_TEXTTYPE == typeId);
                    }
                    else
                    {
                        query =
                            query
                            .Where(s => s.FK_TEXTTYPE == null);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search.LanguageId))
                {
                    query =
                        query.Where(r => r.FK_LANGUAGE == search.LanguageId);
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
                    return OperationResult<IList<TextRevisionDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALLREVISIONS", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALLREVISIONS", ex);
                return OperationResult<IList<TextRevisionDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALLREVISIONS", "GENERIC_ERROR") });
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
                    return OperationResult<TextDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<TextDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<TextDTO>> Save(TextDTO dto)
        {
            try
            {
                if (dto.Id.HasValue)
                {
                    var entity = await _textRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.FK_TEXTTYPE = dto.TextTypeId;
                        entity.ACTIVE = dto.Active;
                        entity.FK_COUNTRY = dto.Country;
                        entity.FK_LANGUAGE = dto.Language;
                        entity.FK_PRICELIST = dto.Site;
                        entity.FK_TEXTTYPE = dto.TextTypeId;
                        entity.FK_WEBSITE = dto.Website;
                        entity.NAME = dto.Name;

                        var revisions = _textRevisionRepository.Repository
                            .Where(tt => tt.FK_TEXT == entity.ID)
                            .OrderByDescending(d => d.CREATION_DATE)
                            .ToList();

                        var lastRevision = revisions.FirstOrDefault();
                        if (lastRevision != null && string.Equals(lastRevision.CONTENT, dto.TextRevision.Content, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            var revision = _mapper.Map<TextRevision>(dto.TextRevision);
                            revision.TEXT = entity;
                            revision.FK_TEXT = entity.ID;
                            revision.REVISION_NUMBER = revisions.Max(r => r.REVISION_NUMBER) + 1;

                            await _textRevisionRepository.Create(revision);

                            revisions.Add(revision);
                        }

                        entity.REVISIONS = revisions;
                        var result = await _textRepository.Update(entity);

                        var r = _mapper.Map<TextDTO>(result);
                        return OperationResult<TextDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<TextDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                    }
                }
                else
                {
                    var existRs = await NotDuplicated(dto);

                    if (existRs.Success)
                    {
                        var entity = _mapper.Map<Text>(dto);
                        var revision = _mapper.Map<TextRevision>(dto.TextRevision);
                        entity.ACTIVE = true;
                        entity.REVISIONS.Add(revision);
                        await _textRepository.Create(entity);

                        var r = _mapper.Map<TextDTO>(entity);

                        return OperationResult<TextDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<TextDTO>.MakeFailure(existRs.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<TextDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
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

                var resx = query.Count();

                return await Task.FromResult(resx == 0 ? OperationResult.MakeSuccess() : OperationResult.MakeFailure(new[] {ErrorMessage.Create("DUPLICATED", "DUPLICATED") }));
            }catch(Exception ex)
            {
                _logger.LogError("EXIST", ex);
                return OperationResult<TextDTO>.MakeFailure(new[] { ErrorMessage.Create("EXIST", "GENERIC_ERROR") });
            }
        }

        //public IDictionary<string, List<ResourceDto>> GenerateResources(string idVendor, InstanceDTO istanza, IList<ResourceTypeDto> resourceTypes)
        //{
        //    var idIstanza = istanza.InstanceId;
        //    var languages = istanza.Languages.Select(l => l.LanguageId).ToList();

        //    Dictionary<string, List<ResourceDto>> generatedResources = new Dictionary<string, List<ResourceDto>>();
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        var rts = resourceTypes.Select(r => r.Id).Distinct().ToArray();

        //        var allresources =
        //            context.Risorse
        //            .Where(s => s.ATTIVA == true && (s.FK_TIPOLOGIA == null || rts.Contains(s.FK_TIPOLOGIA)) &&
        //                ((string.IsNullOrEmpty(s.FK_VENDOR)) ||
        //                (s.FK_VENDOR == idVendor && string.IsNullOrEmpty(s.FK_ISTANZA)) ||
        //                (!string.IsNullOrEmpty(s.FK_VENDOR) && !string.IsNullOrEmpty(s.FK_ISTANZA) && s.FK_ISTANZA == idIstanza)))
        //            .ToList();

        //        foreach (var l in languages)
        //        {
        //            var resources =
        //                allresources
        //                .Where(n => n.FK_LINGUA == l)
        //                .OrderBy(n => n.NOME)
        //                .Select(r => new ResourceDto()
        //                {
        //                    Id = r.ID,
        //                    Vendor = r.FK_VENDOR,
        //                    Istanza = r.FK_ISTANZA,
        //                    Nazione = r.FK_NAZIONE,
        //                    IdTipologia = r.FK_TIPOLOGIA,
        //                    Nome = r.NOME,
        //                    Revisione = GetLastRevisionDto(r, context),
        //                    Tipologia = new ResourceTypeDto() { Nome = r.TIPOLOGIA != null && !string.IsNullOrEmpty(r.TIPOLOGIA.TIPOLOGIA) ? r.TIPOLOGIA.TIPOLOGIA : string.Empty, Subfolder = r.TIPOLOGIA != null ? r.TIPOLOGIA.SUBFOLDER : string.Empty },
        //                })
        //                .ToList();

        //            var types = resources
        //                .Select(t => t.IdTipologia)
        //                .Distinct()
        //                .ToList();

        //            List<ResourceDto> list = new List<ResourceDto>();
        //            foreach (var t in types)
        //            {
        //                List<ResourceDto> listForType = new List<ResourceDto>();

        //                var resourcesForType =
        //                    resources
        //                    .Where(rft => rft.IdTipologia == t)
        //                    .ToList();

        //                foreach (var n in resourcesForType.Select(s => s.Nome).Distinct())
        //                {
        //                    var resourcesByName =
        //                        resourcesForType
        //                        .Where(resx => resx.Nome.Equals(n, StringComparison.InvariantCultureIgnoreCase))
        //                        .ToList();

        //                    if (resourcesByName.Count() == 1)
        //                    {
        //                        listForType.Add(resourcesByName.First());
        //                    }
        //                    else
        //                    {
        //                        var resourcesByVendor =
        //                            resourcesByName.Where(resx => resx.Vendor == idVendor)
        //                            .ToList();

        //                        var globalResource =
        //                            resourcesByName.Where(resx => string.IsNullOrEmpty(resx.Vendor))
        //                            .ToList();

        //                        if (resourcesByVendor.Count() == 1)
        //                        {
        //                            listForType.Add(resourcesByVendor.First());
        //                        }
        //                        else
        //                        {
        //                            var resourcesByInstance =
        //                                resourcesByVendor.Where(resx => resx.Istanza == idIstanza)
        //                                .ToList();

        //                            if (resourcesByInstance.Count() == 0)
        //                            {
        //                                var resourceCustomByInstance =
        //                                    resourcesByVendor.Where(resx => !string.IsNullOrEmpty(resx.Vendor) && !string.IsNullOrEmpty(resx.Istanza))
        //                                    .FirstOrDefault();

        //                                if (resourceCustomByInstance != null)
        //                                {
        //                                    listForType.Add(resourceCustomByInstance);
        //                                }
        //                                else
        //                                {
        //                                    if (globalResource.Count == 1)
        //                                    {
        //                                        listForType.Add(globalResource.First());
        //                                    }
        //                                    else
        //                                    {
        //                                        var nations =
        //                                            globalResource.Select(ris => ris.Nazione)
        //                                            .Distinct()
        //                                            .ToList();

        //                                        foreach (var nat in nations)
        //                                        {
        //                                            var resxForNation = resourcesByName
        //                                                .Where(resx => resx.Nazione == nat)
        //                                                .ToList();

        //                                            if (resxForNation.Count == 1)
        //                                            {
        //                                                listForType.Add(resxForNation.First());
        //                                            }
        //                                            else
        //                                            {
        //                                                var resx =
        //                                                    resxForNation.Where(ris => ris.Istanza == idIstanza)
        //                                                    .Single();

        //                                                listForType.Add(resx);
        //                                            }
        //                                        }
        //                                    }
        //                                }
        //                            }
        //                            else
        //                            {
        //                                if (resourcesByInstance.Count == 1)
        //                                {
        //                                    listForType.Add(resourcesByInstance.First());
        //                                }
        //                                else
        //                                {

        //                                    var nations =
        //                                        resourcesByName.Select(ris => ris.Nazione)
        //                                        .Distinct()
        //                                        .ToList();

        //                                    foreach (var nat in nations)
        //                                    {
        //                                        var resxForNation = resourcesByName
        //                                            .Where(resx => resx.Nazione == nat)
        //                                            .ToList();

        //                                        if (resxForNation.Count == 1)
        //                                        {
        //                                            listForType.Add(resxForNation.First());
        //                                        }
        //                                        else
        //                                        {
        //                                            var resx =
        //                                                resxForNation.Where(ris => ris.Istanza == idIstanza)
        //                                                .Single();

        //                                            listForType.Add(resx);
        //                                        }
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                list.AddRange(listForType);
        //            }

        //            generatedResources.Add(l, list);
        //        }

        //        return generatedResources;
        //    }
        //}

        //public int ImportGlobalResources(string email, ResourceTypeDto type, List<ResourceDto> resourcesForType, bool sovrascrivi)
        //{
        //    var counter = 0;
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        using (var dbContextTransaction = context.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                ResourceCRUD resourceCRUD = new ResourceCRUD();
        //                foreach (var r in resourcesForType)
        //                {
        //                    var resx = context
        //                        .Risorse
        //                        .Where(rx =>
        //                            rx.FK_TIPOLOGIA == type.Id
        //                            && rx.NOME.Equals(r.Nome, StringComparison.InvariantCultureIgnoreCase)
        //                            && rx.FK_LINGUA == r.Lingua
        //                            && rx.FK_VENDOR == r.Vendor
        //                            && rx.FK_ISTANZA == r.Istanza
        //                            && rx.FK_NAZIONE == r.Nazione
        //                        )
        //                        .FirstOrDefault();

        //                    var exist = resx != null;

        //                    if ((exist == false) || (exist == true && sovrascrivi))
        //                    {
        //                        if (exist)
        //                        {
        //                            r.Id = resx.ID;
        //                        }

        //                        resourceCRUD.Save(r);
        //                        counter++;
        //                    }
        //                }
        //                dbContextTransaction.Commit();
        //            }
        //            catch (Exception ex)
        //            {
        //                if (context.Database.Connection.State == System.Data.ConnectionState.Open)
        //                {
        //                    dbContextTransaction.Rollback();
        //                }
        //                return 0;
        //            }
        //        }
        //    }
        //    return counter;
        //}

        //public int ImportDedicatedResources(string email, List<ResourceDto> dedicatedResources, bool sovrascrivi)
        //{
        //    var counter = 0;
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        using (var dbContextTransaction = context.Database.BeginTransaction())
        //        {
        //            try
        //            {

        //                ResourceCRUD resourceCRUD = new ResourceCRUD();
        //                foreach (var r in dedicatedResources)
        //                {
        //                    var resx = context
        //                        .Risorse
        //                        .Where(rx =>
        //                            rx.NOME.Equals(r.Nome, StringComparison.InvariantCultureIgnoreCase)
        //                            && rx.FK_LINGUA == r.Lingua
        //                            && rx.FK_TIPOLOGIA == r.IdTipologia
        //                            && rx.FK_VENDOR == r.Vendor
        //                            && rx.FK_ISTANZA == r.Istanza
        //                            && rx.FK_NAZIONE == r.Nazione
        //                        ).FirstOrDefault();

        //                    var exist = resx != null;

        //                    if ((exist == false) || (exist == true && sovrascrivi))
        //                    {
        //                        if (exist)
        //                        {
        //                            r.Id = resx.ID;
        //                        }

        //                        resourceCRUD.Save(r);
        //                        counter++;
        //                    }
        //                }
        //                dbContextTransaction.Commit();
        //            }
        //            catch (Exception ex)
        //            {
        //                if (context.Database.Connection.State == System.Data.ConnectionState.Open)
        //                {
        //                    dbContextTransaction.Rollback();
        //                }

        //                return 0;
        //            }
        //            return counter;
        //        }
        //    }
        //}

        //public IList<ResourceDto> GetAll(FilterDto resourcesFilterDto)
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        IList<Resource> resources =
        //            GetTextsByFilter(context, resourcesFilterDto)
        //            .OrderBy(x => x.NOME)
        //            .ThenBy(x => x.FK_VENDOR)
        //            .ThenBy(x => x.FK_ISTANZA)
        //            .ThenBy(x => x.FK_LINGUA)
        //                .ToList();

        //        List<ResourceDto> list = new List<ResourceDto>();
        //        foreach (var r in resources)
        //        {
        //            var revision = GetLastRevision(r, context);

        //            ResourceDto dto = new ResourceDto()
        //            {
        //                Id = r.ID,
        //                Nome = r.NOME.ToUpper(),
        //                Istanza = r.FK_ISTANZA,
        //                Lingua = r.FK_LINGUA,
        //                IdTipologia = r.FK_TIPOLOGIA,
        //                Tipologia = r.TIPOLOGIA != null ? new ResourceTypeDto() { Id = r.TIPOLOGIA.ID, Nome = r.TIPOLOGIA.TIPOLOGIA } : null,
        //                Revisione = new RevisionDto() { Numero = revision.REVISIONE, DataCreazione = revision.DATA_CREAZIONE, Testo = revision.TESTO },
        //                Nazione = r.FK_NAZIONE,
        //                Vendor = r.FK_VENDOR,
        //            };
        //            list.Add(dto);
        //        }

        //        return list;
        //    }
        //}

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

        //public IList<ResourceDto> GetLastResources(int numberOfContents, string[] vendors)
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        IList<Resource> resources =
        //            context.Risorse
        //            .Where(r => string.IsNullOrEmpty(r.FK_VENDOR) || (!string.IsNullOrEmpty(r.FK_VENDOR) && vendors.Contains(r.FK_VENDOR)))
        //            .OrderByDescending(r => r.REVISIONI.OrderByDescending(rev => rev.DATA_CREAZIONE).Select(rev => rev.DATA_CREAZIONE).FirstOrDefault())
        //            .Take(numberOfContents)
        //            .ToList();

        //        List<ResourceDto> list = new List<ResourceDto>();

        //        foreach (var r in resources)
        //        {
        //            var dto = new ResourceDto()
        //            {
        //                Id = r.ID,
        //                Lingua = r.FK_LINGUA,
        //                Nome = r.NOME,
        //                Revisione = GetLastRevisionDto(r, context),
        //                Tipologia = r.FK_TIPOLOGIA.HasValue ? new ResourceTypeDto() { Id = r.TIPOLOGIA.ID, Nome = r.TIPOLOGIA.TIPOLOGIA } : null,
        //            };

        //            list.Add(dto);
        //        }
        //        return list;
        //    }
        //}

        //public override void Delete(int id)
        //{
        //    Delete(id, false);
        //}

        //public void Delete(int id, bool caneditglobal)
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        Resource resource =
        //            context.Risorse
        //            .Where(r => r.ID == id)
        //            .Single();

        //        if ((string.IsNullOrEmpty(resource.FK_VENDOR) && caneditglobal)
        //            || (!string.IsNullOrEmpty(resource.FK_VENDOR)))
        //        {

        //            while (resource.REVISIONI.Count > 0)
        //            {
        //                var revision = resource.REVISIONI.First();

        //                resource.REVISIONI.Remove(revision);
        //                context.TestiPerRisorsa.Remove(revision);
        //            }
        //            context.Risorse.Remove(resource);

        //            context.SaveChanges();
        //        }
        //    }
        //}

        //public void CleanRevisions(int number)
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        var resources =
        //            context.Risorse
        //            .Where(r => r.REVISIONI.Count > number)
        //            .ToList();

        //        foreach (var r in resources)
        //        {
        //            var revisions =
        //                r.REVISIONI
        //                .OrderByDescending(s => s.DATA_CREAZIONE)
        //                .Skip(number)
        //                .ToList();

        //            foreach (var rev in revisions)
        //            {
        //                context.TestiPerRisorsa.Remove(rev);
        //            }

        //            context.SaveChanges();
        //        }


        //    }
        //}

        //public int GetResourcesCount()
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        var count =
        //            context.Risorse
        //            .Where(r => r.ATTIVA == true)
        //            .Count();

        //        return count;
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
