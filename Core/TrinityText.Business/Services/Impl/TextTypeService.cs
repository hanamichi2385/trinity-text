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
    public class TextTypeService : ITextTypeService
    {
        private readonly IRepository<TextType> _textTypeRepository;

        private readonly ILogger<TextTypeService> _logger;

        private readonly IMapper _mapper;

        public TextTypeService(IRepository<TextType> textTypeRepository, IMapper mapper, ILogger<TextTypeService> logger)
        {
            _textTypeRepository = textTypeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<IList<TextTypeDTO>>> GetAll()
        {
            try
            {
                var list = _textTypeRepository
                    .Repository
                    .OrderBy(t => t.SUBFOLDER)
                    .ThenBy(t => t.CONTENTTYPE)
                    .ToList();

                var result = _mapper.Map<IList<TextTypeDTO>>(list);

                return await Task.FromResult(OperationResult<IList<TextTypeDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALL", ex);
                return OperationResult<IList<TextTypeDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALL", "GENERIC_ERROR") });
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
                    var result = _mapper.Map<TextTypeDTO>(entity);

                    return OperationResult<TextTypeDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<TextTypeDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<TextTypeDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<IList<TextTypeDTO>>> GetAllByWebsite(string website)
        {
            try
            {
                var list = _textTypeRepository
                    .Repository
                    .Where(tt => tt.TEXTTYPEPERWEBSITES.Where(tx => tx.FK_WEBSITE == website).Any())
                    .OrderBy(t => t.SUBFOLDER)
                    .ThenBy(t => t.CONTENTTYPE)
                    .ToList();

                var result = _mapper.Map<IList<TextTypeDTO>>(list);

                return await Task.FromResult(OperationResult<IList<TextTypeDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALL", ex);
                return OperationResult<IList<TextTypeDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETALL", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<TextTypeDTO>> Save(TextTypeDTO dto)
        {
            try
            {
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

                        var r = _mapper.Map<TextTypeDTO>(result);

                        return OperationResult<TextTypeDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<TextTypeDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                    }
                }
                else
                {
                    var entity = _mapper.Map<TextType>(dto);
                    var result = await _textTypeRepository.Create(entity);

                    var r = _mapper.Map<TextTypeDTO>(result);

                    return OperationResult<TextTypeDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<TextTypeDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
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
