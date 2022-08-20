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
    public class CDNSettingService : ICDNSettingService
    {
        private readonly IRepository<CdnServer> _cdnSettingsRepository;

        private readonly IRepository<FtpServerPerCdnServer> _ftpServerPerCdnServerRepository;

        private readonly ILogger<CDNSettingService> _logger;

        private readonly IMapper _mapper;

        public CDNSettingService(IRepository<CdnServer> cdnSettingsRepository, IRepository<FtpServerPerCdnServer> ftpServerPerCdnServerRepository, IMapper mapper, ILogger<CDNSettingService> logger)
        {
            _cdnSettingsRepository = cdnSettingsRepository;
            _ftpServerPerCdnServerRepository = ftpServerPerCdnServerRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<IList<CdnServerDTO>>> GetAll()
        {
            try
            {
                var list = _cdnSettingsRepository
                    .Repository
                    .OrderBy(t => t.TYPE)
                    .ToList();

                var result = _mapper.Map<IList<CdnServerDTO>>(list);

                return await Task.FromResult(OperationResult<IList<CdnServerDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALL", ex);
                return OperationResult<IList<CdnServerDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALL", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<IList<CdnServerDTO>>> GetAllByWebsite(string website)
        {
            try
            {
                var list = _cdnSettingsRepository
                    .Repository
                    .Where(c => c.CDNSERVERPERWEBSITES.Where(w => w.FK_WEBSITE == website).Any())
                    .OrderBy(t => t.TYPE)
                    .ToList();

                var result = _mapper.Map<IList<CdnServerDTO>>(list);

                return await Task.FromResult(OperationResult<IList<CdnServerDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALL", ex);
                return OperationResult<IList<CdnServerDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALL", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<CdnServerDTO>> Get(int id)
        {
            try
            {
                var entity = await _cdnSettingsRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = _mapper.Map<CdnServerDTO>(entity);

                    return OperationResult<CdnServerDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<CdnServerDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<CdnServerDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }
        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _cdnSettingsRepository
                    .Read(id);

                if (entity != null)
                {
                    await _cdnSettingsRepository.Delete(entity);

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

        public async Task<OperationResult<CdnServerDTO>> Save(CdnServerDTO dto, IList<int> ftpList)
        {
            try
            {
                if (dto.Id.HasValue)
                {
                    var entity = await _cdnSettingsRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.BASEURL = dto.BaseUrl;
                        entity.NAME = dto.Name;
                        entity.TYPE = dto.IdType.Value;

                        var cpv =
                            entity.FTPSERVERS
                            .ToList();

                        foreach (var c in cpv)
                        {
                            var cdnRemove = ftpList.Where(k => k == c.FK_FTPSERVER).Any();

                            if (!cdnRemove)
                            {
                                entity.FTPSERVERS.Remove(c);
                            }
                        }

                        foreach (var c in ftpList)
                        {
                            var fc = new FtpServerPerCdnServer()
                            {
                                FK_FTPSERVER = c,
                                FK_CDNSERVER = entity.ID,
                            };
                            entity.FTPSERVERS.Add(fc);
                        }

                        var result = await _cdnSettingsRepository.Update(entity);

                        var r = _mapper.Map<CdnServerDTO>(result);

                        return OperationResult<CdnServerDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<CdnServerDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                    }
                }
                else
                {
                    var entity = _mapper.Map<CdnServer>(dto);
                    foreach (var c in ftpList)
                    {
                        var fc = new FtpServerPerCdnServer()
                        {
                            FK_FTPSERVER = c,
                            FK_CDNSERVER = entity.ID,
                        };
                        entity.FTPSERVERS.Add(fc);
                    }
                    var result = await _cdnSettingsRepository.Create(entity);

                    var r = _mapper.Map<CdnServerDTO>(result);

                    return OperationResult<CdnServerDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<CdnServerDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
            }
        }


    }
}
