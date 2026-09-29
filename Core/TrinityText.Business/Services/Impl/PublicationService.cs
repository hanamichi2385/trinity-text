using AutoMapper;
using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.Business.Services.Impl
{
    public class PublicationService : IPublicationService
    {
        private readonly IRepository<Publication> _publicationRepository;

        private readonly IRepository<FtpServer> _ftpServerRepository;

        private readonly ILogger<PublicationService> _logger;

        private readonly IMapper _mapper;

        public PublicationService(IRepository<Publication> publicationRepository, IRepository<FtpServer> ftpServerRepository, IMapper mapper, ILogger<PublicationService> logger)
        {
            _publicationRepository = publicationRepository;
            _ftpServerRepository = ftpServerRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<PublicationDTO>> Get(int id, bool withContent)
        {
            try
            {
                var entity = await _publicationRepository
                    .Read(id);

                if(entity != null)
                {
                    var bytes = default(byte[]);
                    if (withContent)
                    {
                        bytes = await GetZipContent(id);
                    }

                    var result = new PublicationDTO()
                    {
                        Id = entity.ID,
                        StatusMessage = entity.STATUS_MESSAGE,
                        Email = entity.EMAIL,
                        DataType = (PublicationType)entity.DATATYPE,
                        LastUpdate = entity.LASTUPDATE_DATE,
                        CreationUser = entity.CREATION_USER,
                        FtpServer = entity.FK_FTPSERVER.HasValue ? new FTPServerDTO() { Id = entity.FTPSERVER.ID, Host = entity.FTPSERVER.HOST, Name = entity.FTPSERVER.NAME, Password = entity.FTPSERVER.PASSWORD, Port = entity.FTPSERVER.PORT, Username = entity.FTPSERVER.USERNAME } : null,
                        Website = entity.FK_WEBSITE,
                        ZipFile = withContent ? bytes : null,
                        HasZipFile = bytes != null && bytes.Length > 0,
                        ManualDelete = entity.MANUALDELETE,
                        FilterDataDate = entity.FILTERDATA_DATE,
                        StatusCode = (PublicationStatus)entity.STATUS_CODE,
                        CdnServer = entity.FK_CDNSERVER.HasValue ? new CdnServerDTO() { Id = entity.CDNSERVER.ID, BaseUrl = entity.CDNSERVER.BASEURL, Name = entity.CDNSERVER.NAME, Type = (EnvironmentType)entity.CDNSERVER.TYPE } : null,
                        Format = (PublicationFormat)entity.FORMAT,
                    };
                    result.SetPayload(entity.PAYLOAD);

                    return OperationResult<PublicationDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<PublicationDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<PublicationDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        private async Task<byte[]> GetZipContent(int id)
        {
            byte[] bytes = null;
            using var sqlConnection = new SqlConnection(_publicationRepository.ConnectionString);
            await sqlConnection.OpenAsync();
            using var sqlCommand = new SqlCommand(@"SELECT [ZIP_FILE] FROM [dbo].[Generazioni] WHERE ID = @id", sqlConnection);
            sqlCommand.Parameters.Add(new SqlParameter("id", id));

            using var reader = await sqlCommand.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess);
            if (await reader.ReadAsync() && !await reader.IsDBNullAsync(0))
            {
                bytes = await reader.GetFieldValueAsync<byte[]>(0);
            }
            return bytes;
        }

        private async Task UpdateZipContent(int id, object zipFile)
        {
            try
            {
                using var sqlConnection = new SqlConnection(_publicationRepository.ConnectionString);
                await sqlConnection.OpenAsync();
                using var sqlCommand = new SqlCommand(@"UPDATE [dbo].[Generazioni] SET [ZIP_FILE] = @zip  WHERE ID = @id", sqlConnection);
                sqlCommand.Parameters.Add(new SqlParameter("id", id));
                // byte[] or Stream (streamed to the server, VarBinary(max))
                sqlCommand.Parameters.Add(new SqlParameter("zip", System.Data.SqlDbType.VarBinary, -1) { Value = zipFile });

                await sqlCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UPDATE_ZIP_CONTENT {id} : {message}", id, ex.Message);
                // propagate: Update() reports a failure instead of a publication that looks successful without ZIP
                throw;
            }
        }

        public async Task<OperationResult<IList<PublicationDTO>>> GetAll(string[] websites = null)
        {
            try
            {
                // null = no tenant restriction (backward compatible); pass the caller's websites to scope the result
                var query = _publicationRepository.Repository;
                if (websites != null)
                {
                    query = query.Where(f => websites.Contains(f.FK_WEBSITE));
                }

                var list = await _publicationRepository.ToListAsync(query
                    .OrderByDescending(f => f.LASTUPDATE_DATE)
                    .Select(f => new
                    {
                        ID = f.ID,
                        STATUS_MESSAGE = f.STATUS_MESSAGE,
                        PUBLICATIONTYPE = f.DATATYPE,
                        LASTUPDATE_DATE = f.LASTUPDATE_DATE,
                        CREATION_USER = f.CREATION_USER,
                        FTP_ID = f.FK_FTPSERVER,
                        WEBSITE = f.FK_WEBSITE,
                        MANUALDELETE = f.MANUALDELETE,
                        FILTERDATA_DATE = f.FILTERDATA_DATE,
                        STATUS_CODE = f.STATUS_CODE,
                        HAS_FILEZIP = false,
                        FORMAT=f.FORMAT,
                    }));

                // FTP names are read with a separate, narrow query: navigating f.FTPSERVER inside the projection
                // could become an inner join (dropping publications without FTP server) depending on the provider
                var ftpIds = list.Where(f => f.FTP_ID.HasValue).Select(f => f.FTP_ID.Value).Distinct().ToArray();
                var ftpNames = ftpIds.Length == 0
                    ? new Dictionary<int, string>()
                    : (await _ftpServerRepository.ToListAsync(
                        _ftpServerRepository
                            .Repository
                            .Where(s => ftpIds.Contains(s.ID))
                            .Select(s => new { s.ID, s.NAME })))
                        .ToDictionary(s => s.ID, s => s.NAME);

                var result = 
                    list
                    .Select(f => new PublicationDTO()
                    {
                        Id = f.ID,
                        StatusMessage = f.STATUS_MESSAGE,
                        DataType = (PublicationType)f.PUBLICATIONTYPE,
                        Format = (PublicationFormat)f.FORMAT,
                        StatusCode = (PublicationStatus)f.STATUS_CODE,
                        LastUpdate = f.LASTUPDATE_DATE,
                        CreationUser = f.CREATION_USER,
                        FtpServer = f.FTP_ID.HasValue ? new FTPServerDTO() { Name = ftpNames.GetValueOrDefault(f.FTP_ID.Value) } : null,
                        Website = f.WEBSITE,
                        ManualDelete = f.MANUALDELETE,
                        FilterDataDate = f.FILTERDATA_DATE,
                        HasZipFile = f.HAS_FILEZIP,
                    })
                    .ToList();

                return OperationResult<IList<PublicationDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<PublicationDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _publicationRepository
                    .Read(id);

                if (entity != null)
                {
                    await _publicationRepository.Delete(entity);

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

        public async Task<OperationResult<PublicationDTO>> Create(PublicationDTO dto)
        {
            try
            {
                var entity = new Publication();
                if (dto.FtpServer != null)
                {
                    entity.FK_FTPSERVER = dto.FtpServer.Id;
                }

                if (dto.CdnServer != null)
                {
                    entity.FK_CDNSERVER = dto.CdnServer.Id;
                }

                entity.FK_WEBSITE = dto.Website;
                entity.EMAIL = dto.Email;
                entity.DATATYPE = (int)dto.DataType;
                entity.STATUS_CODE = (int)PublicationStatus.Created;
                entity.LASTUPDATE_DATE = DateTime.Now;
                entity.MANUALDELETE = dto.ManualDelete;
                entity.FILTERDATA_DATE = dto.FilterDataDate;
                entity.PAYLOAD = dto.GetPayload();
                entity.CREATION_USER = dto.CreationUser;
                entity.FORMAT = (int)dto.Format;
                entity.STATUS_MESSAGE = dto.StatusMessage;

                var  saved = await _publicationRepository.Create(entity);

                var result = _mapper.Map<PublicationDTO>(saved);
                result.SetPayload(entity.PAYLOAD);

                return OperationResult<PublicationDTO>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CREATE {message}", ex.Message);
                return OperationResult<PublicationDTO>.MakeFailure([ErrorMessage.Create("CREATE", "GENERIC_ERROR")]);
            }
        }

        public Task<OperationResult> Update(int id, PublicationStatus status, string message, byte[] zipFile)
            => UpdateInternal(id, status, message, zipFile);

        public Task<OperationResult> UpdateWithZipStream(int id, PublicationStatus status, string message, System.IO.Stream zipFile)
            => UpdateInternal(id, status, message, zipFile);

        private async Task<OperationResult> UpdateInternal(int id, PublicationStatus status, string message, object zipFile)
        {
            try
            {
                var statusCode = (int)status;

                // targeted UPDATE of the two status columns: no entity (with CDN / FTP graph) is loaded and rewritten
                var updated = await _publicationRepository.ExecuteUpdateAsync(
                    _publicationRepository.Repository.Where(p => p.ID == id),
                    set => set
                        .Set(p => p.STATUS_CODE, statusCode)
                        .Set(p => p.STATUS_MESSAGE, message));

                if (updated > 0)
                {
                    _logger.LogInformation("Update publication {id} status {status}: {message}", id, status, message);

                    if (zipFile != null)
                    {
                        await UpdateZipContent(id, zipFile);
                    }

                    return OperationResult.MakeSuccess();
                }
                else
                {
                    return OperationResult.MakeFailure([ErrorMessage.Create("UPDATE", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UPDATE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("UPDATE", "GENERIC_ERROR")]);
            }
        }
    }
}
