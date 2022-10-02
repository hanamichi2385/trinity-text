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
    public class FTPServerService : IFTPServerService
    {
        private readonly IRepository<FtpServer> _ftpServerRepository;

        private readonly ILogger<FTPServerService> _logger;

        private readonly IMapper _mapper;

        public FTPServerService(IRepository<FtpServer> ftpServerRepository, IMapper mapper, ILogger<FTPServerService> logger)
        {
            _ftpServerRepository = ftpServerRepository;
            _mapper = mapper;
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
                    var result = _mapper.Map<FTPServerDTO>(entity);

                    return OperationResult<FTPServerDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<FTPServerDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<FTPServerDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<IList<FTPServerDTO>>> GetAll()
        {
            try
            {
                var list = _ftpServerRepository
                    .Repository
                    .OrderBy(t => t.TYPE)
                    .ToList();

                var result = _mapper.Map<IList<FTPServerDTO>>(list);

                return await Task.FromResult(OperationResult<IList<FTPServerDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALL", ex);
                return OperationResult<IList<FTPServerDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALL", "GENERIC_ERROR") });
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
                    return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("REMOVE", ex);
                return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<FTPServerDTO>> Save(FTPServerDTO dto)
        {
            try
            {
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

                        var r = _mapper.Map<FTPServerDTO>(result);

                        return OperationResult<FTPServerDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<FTPServerDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                    }
                }
                else
                {
                    var entity = _mapper.Map<FtpServer>(dto);
                    var result = await _ftpServerRepository.Create(entity);

                    var r = _mapper.Map<FTPServerDTO>(result);

                    return OperationResult<FTPServerDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<FTPServerDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
            }
        }
    }
}
