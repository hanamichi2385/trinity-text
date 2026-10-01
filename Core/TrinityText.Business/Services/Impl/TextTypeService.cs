using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.Business.Services.Impl
{
    public class TextTypeService : ITextTypeService
    {
        private readonly IRepository<TextType> _textTypeRepository;

        private readonly ILogger<TextTypeService> _logger;

        public TextTypeService(IRepository<TextType> textTypeRepository, ILogger<TextTypeService> logger)
        {
            _textTypeRepository = textTypeRepository;
            _logger = logger;
        }

        public async Task<OperationResult<IList<TextTypeDTO>>> GetAll()
        {
            try
            {
                var list = await _textTypeRepository.ToListAsync(_textTypeRepository.Repository
                    .OrderBy(t => t.SUBFOLDER)
                    .ThenBy(t => t.CONTENTTYPE));

                var result = BusinessMapper.ToDtoList(list);

                return OperationResult<IList<TextTypeDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<TextTypeDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<TextTypeDTO>> Get(int id)
        {
            try
            {
                var entity = await _textTypeRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = BusinessMapper.ToDto(entity);

                    return OperationResult<TextTypeDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<TextTypeDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<TextTypeDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<IList<TextTypeDTO>>> GetAllByWebsite(string website)
        {
            try
            {
                var list = await _textTypeRepository.ToListAsync(_textTypeRepository.Repository
                    .Where(tt => tt.TEXTTYPEPERWEBSITES.Any(tx => tx.FK_WEBSITE == website))
                    .OrderBy(t => t.SUBFOLDER)
                    .ThenBy(t => t.CONTENTTYPE));

                var result = BusinessMapper.ToDtoList(list);

                return OperationResult<IList<TextTypeDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<TextTypeDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<TextTypeDTO>> Save(TextTypeDTO dto)
        {
            try
            {
                if (!PathSafety.IsValidSegmentOrEmpty(dto.Subfolder))
                {
                    return OperationResult<TextTypeDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_SUBFOLDER")]);
                }

                // the name is used as export file name
                if (!PathSafety.IsValidSegment(dto.Name))
                {
                    return OperationResult<TextTypeDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_NAME")]);
                }

                if (dto.Id.HasValue)
                {
                    var entity = await _textTypeRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.CONTENTTYPE = dto.Name;
                        entity.NOTE = dto.Note;
                        entity.SUBFOLDER = dto.Subfolder;

                        var result = await _textTypeRepository.Update(entity);

                        var r = BusinessMapper.ToDto(result);

                        return OperationResult<TextTypeDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<TextTypeDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                    }
                }
                else
                {
                    var entity = BusinessMapper.ToEntity(dto);
                    var result = await _textTypeRepository.Create(entity);

                    var r = BusinessMapper.ToDto(result);

                    return OperationResult<TextTypeDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<TextTypeDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> Remove(int id)
        {
            try
            {
                var entity = await _textTypeRepository
                    .Read(id);

                if (entity != null)
                {
                    await _textTypeRepository.Delete(entity);

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
