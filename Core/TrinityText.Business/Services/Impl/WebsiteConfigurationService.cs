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
    public class WebsiteConfigurationService : IWebsiteConfigurationService
    {
        private readonly IRepository<WebsiteConfiguration> _websiteConfigurationRepository;

        private readonly ILogger<WebsiteConfigurationService> _logger;

        private readonly IMapper _mapper;

        public WebsiteConfigurationService(IRepository<WebsiteConfiguration> websiteConfigurationRepository, IMapper mapper, ILogger<WebsiteConfigurationService> logger)
        {
            _websiteConfigurationRepository = websiteConfigurationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<IList<WebsiteConfigurationDTO>>> GetAll(string website)
        {
            try
            {
                var list = _websiteConfigurationRepository
                    .Repository
                    .Where(w => w.FK_WEBSITE == website)
                    .OrderBy(t => t.TYPE)
                    .ToList();

                var result = _mapper.Map<IList<WebsiteConfigurationDTO>>(list);

                return await Task.FromResult(OperationResult<IList<WebsiteConfigurationDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALL", ex);
                return OperationResult<IList<WebsiteConfigurationDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALL", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<WebsiteConfigurationDTO>> Get(int id)
        {
            try
            {
                var entity = await _websiteConfigurationRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = _mapper.Map<WebsiteConfigurationDTO>(entity);

                    return OperationResult<WebsiteConfigurationDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<WebsiteConfigurationDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<WebsiteConfigurationDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<WebsiteConfigurationDTO>> Save(WebsiteConfigurationDTO dto)
        {
            try
            {
                if (dto.Id.HasValue)
                {
                    var entity = await _websiteConfigurationRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.NOTE = dto.Note;
                        entity.TYPE = (int)dto.Type;
                        entity.URL = dto.Url.StartsWith("http") ? dto.Url : "https://" + dto.Url;

                        var result = await _websiteConfigurationRepository.Update(entity);

                        var r = _mapper.Map<WebsiteConfigurationDTO>(result);

                        return OperationResult<WebsiteConfigurationDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<WebsiteConfigurationDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                    }
                }
                else
                {
                    var entity = _mapper.Map<WebsiteConfiguration>(dto);
                    var result = await _websiteConfigurationRepository.Create(entity);

                    var r = _mapper.Map<WebsiteConfigurationDTO>(result);

                    return OperationResult<WebsiteConfigurationDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<WebsiteConfigurationDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _websiteConfigurationRepository
                    .Read(id);

                if (entity != null)
                {
                    await _websiteConfigurationRepository.Delete(entity);

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
