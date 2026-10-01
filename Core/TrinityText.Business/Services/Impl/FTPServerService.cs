using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.Business.Services.Impl
{
    public class FTPServerService : IFTPServerService
    {
        private readonly IRepository<FtpServer> _ftpServerRepository;

        private readonly ILogger<FTPServerService> _logger;

        public FTPServerService(IRepository<FtpServer> ftpServerRepository, ILogger<FTPServerService> logger)
        {
            _ftpServerRepository = ftpServerRepository;
            _logger = logger;
        }

        public async Task<OperationResult<FTPServerDTO>> Get(int id)
        {
            try
            {
                var entity = await _ftpServerRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = BusinessMapper.ToDto(entity);

                    return OperationResult<FTPServerDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<FTPServerDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<FTPServerDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<IList<FTPServerDTO>>> GetAll()
        {
            try
            {
                var list = await _ftpServerRepository.ToListAsync(_ftpServerRepository.Repository
                    .OrderBy(t => t.TYPE));

                var result = BusinessMapper.ToDtoList(list);

                return OperationResult<IList<FTPServerDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<FTPServerDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<IList<FTPServerDTO>>> GetAllByCDN(int cdn)
        {
            try
            {
                var list = await _ftpServerRepository.ToListAsync(_ftpServerRepository.Repository
                    .Where(f => f.CDNSERVERS.Any(c => c.FK_CDNSERVER == cdn))
                    .OrderBy(t => t.TYPE));

                var result = BusinessMapper.ToDtoList(list);

                return OperationResult<IList<FTPServerDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<FTPServerDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _ftpServerRepository
                    .Read(id);

                if (entity != null)
                {
                    await _ftpServerRepository.Delete(entity);

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

        public async Task<OperationResult<FTPServerDTO>> Save(FTPServerDTO dto)
        {
            try
            {
                // "ftp://host[:port][/dir]" or "sftp://host...": credentials have their own fields and must not be in the address
                if (!string.IsNullOrWhiteSpace(dto.Host)
                    && !(Uri.TryCreate(dto.Host, UriKind.Absolute, out var hostUri)
                        && (hostUri.Scheme == "ftp" || hostUri.Scheme == "sftp")
                        && string.IsNullOrEmpty(hostUri.UserInfo)))
                {
                    return OperationResult<FTPServerDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_HOST")]);
                }

                if (dto.Id.HasValue)
                {
                    var entity = await _ftpServerRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.HOST = dto.Host;
                        entity.NAME = dto.Name;
                        entity.PASSWORD = dto.Password;
                        entity.PORT = dto.Port;
                        entity.TYPE = (int)dto.Type;
                        entity.USERNAME = dto.Username;

                        var result = await _ftpServerRepository.Update(entity);

                        var r = BusinessMapper.ToDto(result);

                        return OperationResult<FTPServerDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<FTPServerDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                    }
                }
                else
                {
                    var entity = BusinessMapper.ToEntity(dto);
                    var result = await _ftpServerRepository.Create(entity);

                    var r = BusinessMapper.ToDto(result);

                    return OperationResult<FTPServerDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<FTPServerDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }
    }
}
