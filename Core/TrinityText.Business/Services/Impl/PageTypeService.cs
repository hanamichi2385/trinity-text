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

        public PageTypeService(IRepository<PageType> pageTypeRepository, ILogger<PageTypeService> logger)
        {
            _pageTypeRepository = pageTypeRepository;
            _logger = logger;
        }

        public async Task<OperationResult<PageTypeDTO[]>> GetAll()
        {
            try
            {
                var list = await _pageTypeRepository.ToListAsync(_pageTypeRepository.Repository
                    .OrderBy(t => t.NAME));

                var result = BusinessMapper.ToDtoList(list).ToArray();

                return OperationResult<PageTypeDTO[]>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<PageTypeDTO[]>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
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
                    var result = BusinessMapper.ToDto(entity);

                    return OperationResult<PageTypeDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<PageTypeDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<PageTypeDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<IList<PageTypeDTO>>> GetAllByUser(string[] websites, string[] visibilities)
        {
            try
            {
                var entities = await _pageTypeRepository.ToListAsync(_pageTypeRepository.Repository
                    .Where(t => (string.IsNullOrWhiteSpace(t.FK_WEBSITE) || (!string.IsNullOrWhiteSpace(t.FK_WEBSITE) && websites.Contains(t.FK_WEBSITE))))
                    .OrderBy(t => t.NAME));

                if (entities != null)
                {
                    var visibilitySet = new HashSet<string>(visibilities ?? [], StringComparer.InvariantCultureIgnoreCase);
                    var filtered = entities.Where(e =>
                        string.IsNullOrWhiteSpace(e.VISIBILITY) ||
                            e.VISIBILITY.Split('|', StringSplitOptions.RemoveEmptyEntries).Any(visibilitySet.Contains))
                        .ToList();

                    var result = BusinessMapper.ToDtoList(filtered);

                    return OperationResult<IList<PageTypeDTO>>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<IList<PageTypeDTO>>.MakeFailure([ErrorMessage.Create("GET_BYUSER", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<IList<PageTypeDTO>>.MakeFailure([ErrorMessage.Create("GET_BYUSER", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<PageTypeDTO>> Save(PageTypeDTO dto)
        {
            try
            {
                if (!PathSafety.IsValidSegmentOrEmpty(dto.Subfolder))
                {
                    return OperationResult<PageTypeDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_SUBFOLDER")]);
                }

                if (!PathSafety.IsValidSegmentOrEmpty(dto.OutputFilename))
                {
                    return OperationResult<PageTypeDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_OUTPUT_FILENAME")]);
                }

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
                        entity.VISIBILITY = dto.Visibility != null && dto.Visibility.Count > 0 ? string.Join("|", dto.Visibility) : null;

                        var result = await _pageTypeRepository.Update(entity);

                        var r = BusinessMapper.ToDto(result);

                        return OperationResult<PageTypeDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<PageTypeDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                    }
                }
                else
                {
                    var entity = BusinessMapper.ToEntity(dto);
                    var result = await _pageTypeRepository.Create(entity);

                    var r = BusinessMapper.ToDto(result);

                    return OperationResult<PageTypeDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<PageTypeDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
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
                    return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REMOVE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "GENERIC_ERROR")]);
            }
        }
    }
}
