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
    public class PageTypeService : IPageTypeService
    {
        private readonly IRepository<PageType> _pageTypeRepository;

        private readonly ILogger<PageTypeService> _logger;

        private readonly IMapper _mapper;

        public PageTypeService(IRepository<PageType> pageTypeRepository, IMapper mapper, ILogger<PageTypeService> logger)
        {
            _pageTypeRepository = pageTypeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<IList<PageTypeDTO>>> GetAll()
        {
            try
            {
                var list = _pageTypeRepository
                    .Repository
                    .OrderBy(t => t.NAME)
                    .ToList();

                var result = _mapper.Map<IList<PageTypeDTO>>(list);

                return await Task.FromResult(OperationResult<IList<PageTypeDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALL", ex);
                return OperationResult<IList<PageTypeDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALL", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<PageTypeDTO>> Get(int id)
        {
            try
            {
                var entity = await _pageTypeRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = _mapper.Map<PageTypeDTO>(entity);

                    return OperationResult<PageTypeDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<PageTypeDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<PageTypeDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<IList<PageTypeDTO>>> GetAllByUser(string[] websites, string[] visibilities)
        {
            try
            {
                var entities = _pageTypeRepository
                    .Repository
                    .Where(t => (string.IsNullOrWhiteSpace(t.FK_WEBSITE) || (!string.IsNullOrWhiteSpace(t.FK_WEBSITE) && websites.Contains(t.FK_WEBSITE))))
                    .OrderBy(t => t.NAME)
                    .ToList();

                if (entities != null)
                {
                    var filtered = entities.Where(e => 
                        (string.IsNullOrWhiteSpace(e.VISIBILITY) || 
                            (!string.IsNullOrWhiteSpace(e.VISIBILITY) 
                                && visibilities.Intersect(e.VISIBILITY.Split(',', StringSplitOptions.RemoveEmptyEntries)).Any())))
                        .ToList();

                    var result = _mapper.Map<IList<PageTypeDTO>>(filtered);

                    return await Task.FromResult(OperationResult<IList<PageTypeDTO>>.MakeSuccess(result));
                }
                else
                {
                    return OperationResult<IList<PageTypeDTO>>.MakeFailure(new[] { ErrorMessage.Create("GET_BYUSER", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<IList<PageTypeDTO>>.MakeFailure(new[] { ErrorMessage.Create("GET_BYUSER", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<PageTypeDTO>> Save(PageTypeDTO dto)
        {
            try
            {
                if (dto.Id.HasValue)
                {
                    var entity = await _pageTypeRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.NAME = dto.Name;
                        entity.SCHEMA = dto.Schema;
                        entity.SUBFOLDER = dto.Subfolder;
                        entity.FK_WEBSITE = dto.Website;
                        entity.PATH_PREVIEWPAGE = dto.PathPreviewPage;
                        entity.OUTPUT_FILENAME = dto.OutputFilename;
                        entity.PRINT_ELEMENT_NAME = dto.PrintElementName;
                        entity.VISIBILITY = dto.Visibility != null && dto.Visibility.Count() > 0 ? string.Join("|", dto.Visibility) : null;

                        var result = await _pageTypeRepository.Update(entity);

                        var r = _mapper.Map<PageTypeDTO>(result);

                        return OperationResult<PageTypeDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<PageTypeDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                    }
                }
                else
                {
                    var entity = _mapper.Map<PageType>(dto);
                    var result = await _pageTypeRepository.Create(entity);

                    var r = _mapper.Map<PageTypeDTO>(result);

                    return OperationResult<PageTypeDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<PageTypeDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
            }
        }
        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _pageTypeRepository
                    .Read(id);

                if (entity != null)
                {
                    await _pageTypeRepository.Delete(entity);

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
