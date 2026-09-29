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

        public async Task<OperationResult<IList<CdnServerDTO>>> GetAll()
        {
            try
            {
                var list = await _cdnSettingsRepository.ToListAsync(_cdnSettingsRepository.Repository
                    .OrderBy(t => t.TYPE)
                    );

                var result = _mapper.Map<IList<CdnServerDTO>>(list);

                return OperationResult<IList<CdnServerDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<CdnServerDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<IList<CdnServerDTO>>> GetAllByWebsite(string website)
        {
            try
            {
                var list = await _cdnSettingsRepository.ToListAsync(_cdnSettingsRepository.Repository
                    .Where(c => c.CDNSERVERPERWEBSITES.Any(w => w.FK_WEBSITE == website))
                    .OrderBy(t => t.TYPE)
                    );

                var result = _mapper.Map<IList<CdnServerDTO>>(list);

                return OperationResult<IList<CdnServerDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<CdnServerDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
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
                // the base URL is written into the published pages: only http(s) addresses
                if (!string.IsNullOrWhiteSpace(dto.BaseUrl)
                    && !(Uri.TryCreate(dto.BaseUrl, UriKind.Absolute, out var baseUri)
                        && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps)))
                {
                    return OperationResult<CdnServerDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_URL")]);
                }

                var desiredSet = ftpList.ToHashSet();

                if (dto.Id.HasValue)
                {
                    var cdnId = dto.Id.Value;
                    var exists = await _cdnSettingsRepository.CountAsync(
                        _cdnSettingsRepository.Repository.Where(c => c.ID == cdnId)) > 0;

                    if (exists)
                    {
                        // scalar columns + join table change together or not at all
                        await _cdnSettingsRepository.BeginTransaction();
                        try
                        {
                            var baseUrl = dto.BaseUrl;
                            var name = dto.Name;
                            var type = (int)dto.Type;

                            // targeted UPDATE: updating the loaded entity graph would also rewrite the auto-included FTP servers
                            await _cdnSettingsRepository.ExecuteUpdateAsync(
                                _cdnSettingsRepository.Repository.Where(c => c.ID == cdnId),
                                set => set
                                    .Set(c => c.BASEURL, baseUrl)
                                    .Set(c => c.NAME, name)
                                    .Set(c => c.TYPE, type));

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

                            await _cdnSettingsRepository.CommitTransaction();
                        }
                        catch
                        {
                            await _cdnSettingsRepository.RollbackTransaction();
                            throw;
                        }

                        var result = await _cdnSettingsRepository.FirstOrDefaultAsync(
                            _cdnSettingsRepository.Repository.Where(c => c.ID == cdnId));

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
                    entity.FTPSERVERS = [];

                    CdnServer result;
                    await _cdnSettingsRepository.BeginTransaction();
                    try
                    {
                        result = await _cdnSettingsRepository.Create(entity);

                        // join rows are created from ids once the server has its own id
                        if (desiredSet.Count > 0)
                        {
                            await _ftpServerPerCdnRepository.AddRangeAsync(
                                desiredSet.Select(id => new FtpServerPerCdnServer { FK_CDNSERVER = result.ID, FK_FTPSERVER = id }).ToList());
                        }

                        await _cdnSettingsRepository.CommitTransaction();
                    }
                    catch
                    {
                        await _cdnSettingsRepository.RollbackTransaction();
                        throw;
                    }

                    result = await _cdnSettingsRepository.FirstOrDefaultAsync(
                        _cdnSettingsRepository.Repository.Where(c => c.ID == result.ID));

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
