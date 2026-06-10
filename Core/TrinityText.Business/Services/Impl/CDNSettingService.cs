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

        private readonly IRepository<FtpServerPerCdnServer> _ftpServerPerCdnRepository;

        private readonly ILogger<CDNSettingService> _logger;

        private readonly IMapper _mapper;

        public CDNSettingService(IRepository<CdnServer> cdnSettingsRepository, IRepository<FtpServerPerCdnServer> ftpServerPerCdnRepository, IMapper mapper, ILogger<CDNSettingService> logger)
        {
            _cdnSettingsRepository = cdnSettingsRepository;
            _ftpServerPerCdnRepository = ftpServerPerCdnRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public Task<OperationResult<IList<CdnServerDTO>>> GetAll()
        {
            try
            {
                var list = _cdnSettingsRepository
                    .Repository
                    .OrderBy(t => t.TYPE)
                    .ToList();

                var result = _mapper.Map<IList<CdnServerDTO>>(list);

                return Task.FromResult(OperationResult<IList<CdnServerDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return Task.FromResult(OperationResult<IList<CdnServerDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]));
            }
        }

        public Task<OperationResult<IList<CdnServerDTO>>> GetAllByWebsite(string website)
        {
            try
            {
                var list = _cdnSettingsRepository
                    .Repository
                    .Where(c => c.CDNSERVERPERWEBSITES.Where(w => w.FK_WEBSITE == website).Any())
                    .OrderBy(t => t.TYPE)
                    .ToList();

                var result = _mapper.Map<IList<CdnServerDTO>>(list);

                return Task.FromResult(OperationResult<IList<CdnServerDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return Task.FromResult(OperationResult<IList<CdnServerDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]));
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
                    return OperationResult<CdnServerDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<CdnServerDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
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
                    return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REMOVE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<CdnServerDTO>> Save(CdnServerDTO dto, IList<int> ftpList)
        {
            try
            {
                var desiredSet = ftpList.ToHashSet();

                if (dto.Id.HasValue)
                {
                    var entity = await _cdnSettingsRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.BASEURL = dto.BaseUrl;
                        entity.NAME = dto.Name;
                        entity.TYPE = (int)dto.Type;

                        var cdnId = entity.ID;

                        var existingSet = await _cdnSettingsRepository.ToListAsync(
                            _ftpServerPerCdnRepository
                                .Repository
                                .Where(x => x.FK_CDNSERVER == cdnId)
                                .Select(x => x.FK_FTPSERVER));
                        var existingHash = existingSet.ToHashSet();

                        await _cdnSettingsRepository.ExecuteDeleteAsync(
                            _ftpServerPerCdnRepository
                                .Repository
                                .Where(x => x.FK_CDNSERVER == cdnId && !desiredSet.Contains(x.FK_FTPSERVER)));

                        var toAdd = desiredSet
                            .Except(existingHash)
                            .Select(id => new FtpServerPerCdnServer { FK_CDNSERVER = cdnId, FK_FTPSERVER = id })
                            .ToList();
                        if (toAdd.Count > 0)
                        {
                            await _ftpServerPerCdnRepository.AddRangeAsync(toAdd);
                        }

                        var result = await _cdnSettingsRepository.Update(entity);

                        var r = _mapper.Map<CdnServerDTO>(result);

                        return OperationResult<CdnServerDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<CdnServerDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                    }
                }
                else
                {
                    var entity = _mapper.Map<CdnServer>(dto);
                    foreach (var c in desiredSet)
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
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<CdnServerDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }


    }
}
