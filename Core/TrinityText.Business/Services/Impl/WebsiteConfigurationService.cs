﻿using AutoMapper;
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

        private readonly IRepository<TextTypePerWebsite> _textTypePerWebsiteRepository;

        private readonly IRepository<CdnServersPerWebsite> _cdnServersPerWebsite;

        private readonly ILogger<WebsiteConfigurationService> _logger;

        private readonly IMapper _mapper;

        public WebsiteConfigurationService(IRepository<WebsiteConfiguration> websiteConfigurationRepository, IRepository<TextTypePerWebsite> textTypePerWebsiteRepository, IRepository<CdnServersPerWebsite> cdnServersPerWebsite,
            IMapper mapper, ILogger<WebsiteConfigurationService> logger)
        {
            _websiteConfigurationRepository = websiteConfigurationRepository;
            _textTypePerWebsiteRepository = textTypePerWebsiteRepository;
            _cdnServersPerWebsite = cdnServersPerWebsite;
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
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IList<WebsiteConfigurationDTO>>.MakeFailure([ErrorMessage.Create("GETALL", "GENERIC_ERROR")]);
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
                    return OperationResult<WebsiteConfigurationDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<WebsiteConfigurationDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
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
                        return OperationResult<WebsiteConfigurationDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
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
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<WebsiteConfigurationDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
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
                    return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REMOVE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> AddTo(string website, IList<int> cdnIds, IList<int> textTypeIds)
        {
            

            try
            {
                await _websiteConfigurationRepository.BeginTransaction();

                var cpv =
                    _cdnServersPerWebsite
                    .Repository
                    .Where(f => f.FK_WEBSITE == website)
                    .ToList();

                foreach (var c in cdnIds)
                {
                    var cdnAdd = cpv.Where(k => k.FK_CDNSERVER == c).SingleOrDefault();

                    if (cdnAdd == null)
                    {
                        var n = new CdnServersPerWebsite()
                        {
                            FK_CDNSERVER = c,
                            FK_WEBSITE = website,
                        };
                        await _cdnServersPerWebsite.Create(n);
                    }
                }

                var rtpv =
                    _textTypePerWebsiteRepository
                    .Repository
                    .Where(f => f.FK_WEBSITE == website)
                    .ToList();

                foreach (var c in textTypeIds)
                {
                    var rtAdd = rtpv.Where(k => k.FK_TEXTTYPE == c).SingleOrDefault();

                    if (rtAdd == null)
                    {
                        var n = new TextTypePerWebsite()
                        {
                            FK_TEXTTYPE =c,
                            FK_WEBSITE = website,
                        };

                        await _textTypePerWebsiteRepository.Create(n);
                    }
                }

                foreach (var c in cpv)
                {
                    var cdnRemove = cdnIds.Where(k => k == c.FK_CDNSERVER).Any();

                    if (!cdnRemove)
                    {
                        await _cdnServersPerWebsite.Delete(c);
                    }
                }

                foreach (var c in rtpv)
                {
                    var rtRemove = textTypeIds.Where(k => k == c.FK_TEXTTYPE).Any();

                    if (!rtRemove)
                    {
                        await _textTypePerWebsiteRepository.Delete(c);
                    }
                }

                await _websiteConfigurationRepository.CommitTransaction();

                return OperationResult.MakeSuccess();
            }
            catch(Exception ex)
            {
                await _websiteConfigurationRepository.RollbackTransaction();
                _logger.LogError(ex, "ADDTO {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("ADDTO", "GENERIC_ERROR")]);
            }

            
        }
    }
}
