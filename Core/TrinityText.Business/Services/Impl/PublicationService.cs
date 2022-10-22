using AutoMapper;
using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.Business.Services.Impl
{
    public class PublicationService : IPublicationService
    {
        private readonly IRepository<Publication> _publicationRepository;

        private readonly ILogger<PublicationService> _logger;

        private readonly IMapper _mapper;

        public PublicationService(IRepository<Publication> publicationRepository, IMapper mapper, ILogger<PublicationService> logger)
        {
            _publicationRepository = publicationRepository;
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
                    return OperationResult<PublicationDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<PublicationDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        private async Task<byte[]> GetZipContent(int id)
        {
            var bytes = default(byte[]);
            using (var sqlConnection = new SqlConnection(_publicationRepository.ConnectionString))
            {
                await sqlConnection.OpenAsync();
                using (var sqlCommand = new SqlCommand(@"SELECT [ZIP_FILE] FROM [TRINITY].[dbo].[Generazioni] WHERE ID = @id", sqlConnection))
                {
                    sqlCommand.Parameters.Add(new SqlParameter("id", id));
                    

                    var reader = await sqlCommand.ExecuteReaderAsync();

                    if(reader.Read())
                    {
                        bytes = (byte[])reader["ZIP_FILE"];
                    }
                }
                await sqlConnection.CloseAsync();
            }
            return bytes;
        }

        private async Task UpdateZipContent(int id, byte[] zipFile)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(_publicationRepository.ConnectionString))
                {
                    await sqlConnection.OpenAsync();
                    using (var sqlCommand = new SqlCommand(@"UPDATE [TRINITY].[dbo].[Generazioni] SET [ZIP_FILE] = @zip  WHERE ID = @id", sqlConnection))
                    {
                        sqlCommand.Parameters.Add(new SqlParameter("id", id));
                        sqlCommand.Parameters.Add(new SqlParameter("zip", zipFile));

                        var result = await sqlCommand.ExecuteNonQueryAsync();
                    }
                    await sqlConnection.CloseAsync();
                }
            }catch(Exception ex)
            {

            }
        }

        public async Task<OperationResult<IList<PublicationDTO>>> GetAll()
        {
            try
            {
                var list = _publicationRepository
                    .Repository
                    .OrderByDescending(f => f.LASTUPDATE_DATE)
                    .Select(f => new
                    {
                        ID = f.ID,
                        STATUS_MESSAGE = f.STATUS_MESSAGE,
                        PUBLICATIONTYPE = f.DATATYPE,
                        LASTUPDATE_DATE = f.LASTUPDATE_DATE,
                        CREATION_USER = f.CREATION_USER,
                        FTP_SERVER = f.FTPSERVER,
                        WEBSITE = f.FK_WEBSITE,
                        MANUALDELETE = f.MANUALDELETE,
                        FILTERDATA_DATE = f.FILTERDATA_DATE,
                        STATUS_CODE = f.STATUS_CODE,
                        CDNSERVER = f.CDNSERVER,
                        HAS_FILEZIP = false,
                        FORMAT=f.FORMAT,
                    })
                    .ToList();

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
                        FtpServer = f.FTP_SERVER != null ? new FTPServerDTO() { Name = f.FTP_SERVER.NAME } : null,
                        Website = f.WEBSITE,
                        ManualDelete = f.MANUALDELETE,
                        FilterDataDate = f.FILTERDATA_DATE,
                        HasZipFile = f.HAS_FILEZIP,
                    })
                    .ToList();

                return await Task.FromResult(OperationResult<IList<PublicationDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALL", ex);
                return OperationResult<IList<PublicationDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALL", "GENERIC_ERROR") });
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
                    return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("REMOVE", ex);
                return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "GENERIC_ERROR") });
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
                _logger.LogError("CREATE", ex);
                return OperationResult<PublicationDTO>.MakeFailure(new[] { ErrorMessage.Create("CREATE", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult> Update(int id, PublicationStatus status, string message, byte[] zipFile)
        {
            try
            {
                var entity = await _publicationRepository
                    .Read(id);

                if (entity != null)
                {
                    entity.STATUS_CODE = (int)status;
                    entity.STATUS_MESSAGE = message;

                    //if(zipFile != null)
                    //{
                    //    entity.ZIP_FILE = zipFile;
                    //}

                    



                    await _publicationRepository.Update(entity);

                    if (zipFile != null)
                    {
                        await UpdateZipContent(id, zipFile);
                    }

                    return OperationResult.MakeSuccess();
                }
                else
                {
                    return OperationResult.MakeFailure(new[] { ErrorMessage.Create("UPDATE", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UPDATE", ex);
                return OperationResult.MakeFailure(new[] { ErrorMessage.Create("UPDATE", "GENERIC_ERROR") });
            }
        }

       
    }
}
