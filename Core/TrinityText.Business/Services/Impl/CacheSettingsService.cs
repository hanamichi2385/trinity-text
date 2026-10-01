using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.Business.Services.Impl
{
    public class CacheSettingsService : ICacheSettingsService
    {
        private readonly IRepository<CacheSettings> _cacheSettingsRepository;

        private readonly ILogger<CacheSettingsService> _logger;

        public CacheSettingsService(IRepository<CacheSettings> cacheSettingsRepository, ILogger<CacheSettingsService> logger)
        {
            _cacheSettingsRepository = cacheSettingsRepository;
            _logger = logger;
        }

        public async Task<OperationResult<IList<CacheSettingsDTO>>> GetAll()
        {
            try
            {
                var list = await _cacheSettingsRepository.ToListAsync(_cacheSettingsRepository.Repository
                    .OrderBy(t => t.TYPE));

                var result = BusinessMapper.ToDtoList(list);

                return OperationResult<IList<CacheSettingsDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<CacheSettingsDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<CacheSettingsDTO>> Get(int id)
        {
            try
            {
                var entity = await _cacheSettingsRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = BusinessMapper.ToDto(entity);

                    return OperationResult<CacheSettingsDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<CacheSettingsDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<CacheSettingsDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<CacheSettingsDTO>> GetByCdnServer(int cdnServerId)
        {
            try
            {
                var entity = await _cacheSettingsRepository.FirstOrDefaultAsync(_cacheSettingsRepository.Repository
                    .Where(c => c.FK_CDNSERVER == cdnServerId));

                if (entity != null)
                {
                    var result = BusinessMapper.ToDto(entity);

                    return OperationResult<CacheSettingsDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<CacheSettingsDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<CacheSettingsDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<CacheSettingsDTO>> Save(CacheSettingsDTO dto)
        {
            try
            {
                if (dto.Id.HasValue)
                {
                    var entity = await _cacheSettingsRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.PAYLOAD = dto.Payload;

                        var result = await _cacheSettingsRepository.Update(entity);

                        var r = BusinessMapper.ToDto(result);

                        return OperationResult<CacheSettingsDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<CacheSettingsDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                    }
                }
                else
                {
                    var entity = BusinessMapper.ToEntity(dto);
                    var result = await _cacheSettingsRepository.Create(entity);

                    var r = BusinessMapper.ToDto(result);

                    return OperationResult<CacheSettingsDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<CacheSettingsDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _cacheSettingsRepository
                    .Read(id);

                if (entity != null)
                {
                    await _cacheSettingsRepository.Delete(entity);

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
