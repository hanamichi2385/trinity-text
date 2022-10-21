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
    public class FileManagerService : IFileManagerService
    {
        private readonly IRepository<Folder> _folderRepository;

        private readonly IRepository<File> _fileRepository;

        private readonly ILogger<FileManagerService> _logger;

        private readonly IImageDrawingService _imageDrawingService;

        private readonly IMapper _mapper;

        public FileManagerService(IRepository<Folder> folderRepository, IRepository<File> fileRepository, IImageDrawingService imageDrawingService, IMapper mapper, ILogger<FileManagerService> logger)
        {
            _folderRepository = folderRepository;
            _fileRepository = fileRepository;
            _imageDrawingService = imageDrawingService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OperationResult<IList<FolderDTO>>> GetAllFolders(string[] websites)
        {
            try
            {
                var folders =
                    _folderRepository
                    .Repository
                    .Where(f => websites.Contains(f.FK_WEBSITE))
                    .Select(s => new Folder()
                    {
                        FK_PARENT = s.FK_PARENT,
                        FK_WEBSITE = s.FK_WEBSITE,
                        ID = s.ID,
                        NAME = s.NAME,
                        NOTE = s.NOTE,
                    })
                    .ToList();

                var dtos = _mapper.Map<IList<FolderDTO>>(folders);
                var result = GetFolders(dtos, null);

                return await Task.FromResult(OperationResult<IList<FolderDTO>>.MakeSuccess(result));

            }
            catch (Exception ex)
            {
                _logger.LogError("GET_ALL", ex);
                return OperationResult<IList<FolderDTO>>.MakeFailure(new[] { ErrorMessage.Create("GET_ALL", "GENERIC_ERROR") });
            }
        }

        private IList<FolderDTO> GetFolders(IList<FolderDTO> folders, int? parentId)
        {
            var fs = folders.Where(f => f.ParentId == parentId).ToList();
            foreach (var f in fs)
            {
                f.SubFolders = GetFolders(folders, f.Id);
            }
            return fs;
        }

        public async Task<OperationResult<FolderDTO>> GetFolder(int id)
        {
            try
            {
                var entity = await _folderRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = _mapper.Map<FolderDTO>(entity);

                    return OperationResult<FolderDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<FolderDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<FolderDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<FolderDTO>> SaveFolder(int? parentFolderId, FolderDTO dto)
        {
            try
            {
                if (dto.Id.HasValue)
                {
                    var entity = await _folderRepository
                        .Read(dto.Id.Value);

                    if (entity != null)
                    {
                        entity.NAME = dto.Name;
                        entity.FK_PARENT = parentFolderId;
                        entity.NAME = dto.Name;
                        entity.NOTE = dto.Note;
                        entity.FK_WEBSITE = dto.Website;

                        var result = await _folderRepository.Update(entity);

                        var r = _mapper.Map<FolderDTO>(result);

                        return OperationResult<FolderDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<FolderDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                    }
                }
                else
                {
                    var entity = _mapper.Map<Folder>(dto);
                    entity.DELETABLE = true;
                    entity.FK_PARENT = parentFolderId;
                    var result = await _folderRepository.Create(entity);

                    var r = _mapper.Map<FolderDTO>(result);

                    return OperationResult<FolderDTO>.MakeSuccess(r);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<FolderDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult> RemoveFolder(int id)
        {
            try
            {
                var entity = await _folderRepository
                    .Read(id);

                if (entity != null)
                {
                    await _folderRepository.BeginTransaction();

                    await EmptyFolder(entity);

                    await _folderRepository.CommitTransaction();

                    return OperationResult.MakeSuccess();
                }
                else
                {
                    return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                await _folderRepository.RollbackTransaction();
                _logger.LogError("REMOVE", ex);
                return OperationResult.MakeFailure(new[] { ErrorMessage.Create("REMOVE", "GENERIC_ERROR") });
            }
        }


        private async Task EmptyFolder(Folder folder)
        {
            //while (folder.SUBFOLDERS.Any())
            //{
            //    var subfolder = folder.SUBFOLDERS.First();
            //    await EmptyFolder(subfolder);

            //    folder.SUBFOLDERS.Remove(subfolder);
            //}

            //while (folder.FILES.Count > 0)
            //{
            //    var file = folder.FILES.First();
            //    folder.FILES.Remove(file);

            //    await _fileRepository.Delete(file);
            //}

            await _folderRepository.Delete(folder);
        }

        public async Task<OperationResult<FileDTO>> GetFile(Guid id, bool withThumb)
        {
            try
            {
                var entity = await _fileRepository
                    .Read(id);

                if (entity != null)
                {
                    var result = _mapper.Map<FileDTO>(entity, opts => opts.Items["Content"] = (withThumb ? entity.THUMBNAIL : entity.CONTENT));

                    return OperationResult<FileDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<IList<FileDTO>>> GetFilesByFolder(string website, int id, bool withFileContent, DateTime? lastUpdate)
        {
            try
            {
                var query = _fileRepository
                    .Repository
                    .Where(f => f.FK_WEBSITE == website && f.FK_FOLDER == id);

                if (lastUpdate.HasValue)
                {
                    query = query
                        .Where(f => f.LASTUPDATE_DATE.Date >= lastUpdate.Value.Date);
                }

                var list =
                    query
                    .Select(f => new File()
                    {
                        CONTENT = withFileContent ? f.CONTENT : null,
                        CREATION_DATE = f.CREATION_DATE,
                        CREATION_USER = f.CREATION_USER,
                        FILENAME = f.FILENAME,
                        FK_FOLDER = f.FK_FOLDER,
                        FK_WEBSITE = f.FK_WEBSITE,
                        THUMBNAIL = f.THUMBNAIL,
                        ID = f.ID,
                        LASTUPDATE_DATE = f.LASTUPDATE_DATE,
                        LASTUPDATE_USER = f.LASTUPDATE_USER,
                        //FOLDER = f.FOLDER,
                    })
                    .OrderBy(s => s.FILENAME)
                    .ToList();

                var result = new List<FileDTO>();
                foreach (var f in list)
                {
                    var dto = _mapper.Map<FileDTO>(f);
                    dto.Content = f.CONTENT;
                    result.Add(dto);
                }

                return await Task.FromResult(OperationResult<IList<FileDTO>>.MakeSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<IList<FileDTO>>.MakeFailure(new[] { ErrorMessage.Create("GETFILES_BYFOLDER", "GENERIC_ERROR") });
            }
        }



        public async Task<OperationResult> DeleteFile(Guid id)
        {
            try
            {
                var entity = await _fileRepository
                    .Read(id);

                if (entity != null)
                {
                    await _fileRepository.Delete(entity);

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

        public async Task<OperationResult<string>> GetFileLink(Guid id)
        {
            try
            {
                var entity = await _fileRepository
                    .Read(id);

                if (entity != null)
                {
                    string filePath = $"/{entity.FILENAME}";
                    var currentFolderId =(int?)entity.FK_FOLDER;

                    while(currentFolderId != null)
                    {
                        var folder = _folderRepository
                            .Repository
                            .Where(f => f.ID == currentFolderId.Value)
                            .Select(f => new { Name = f.NAME, ParentId = f.FK_PARENT })
                            .FirstOrDefault();

                        if(folder != null)
                        {
                            filePath = $"/{folder.Name}{filePath}";
                            currentFolderId = folder.ParentId;
                        }
                        else
                        {
                            currentFolderId = null;
                        }
                    }

                    var result = $"@{filePath}";

                    return OperationResult<string>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<string>.MakeFailure(new[] { ErrorMessage.Create("GET", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GET", ex);
                return OperationResult<string>.MakeFailure(new[] { ErrorMessage.Create("GET", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult> AddFile(string user, string website, int folderId, FileDTO dto, bool @override)
        {
            try
            {
                var folder = await _folderRepository
                    .Read(folderId);

                if (folder != null)
                {
                    var content = dto.Content;

                    var compressionRs = await _imageDrawingService.Compression(dto);
                    if (compressionRs.Success)
                    {
                        content = compressionRs.Value;
                    }

                    var thumb = default(byte[]);
                    var thumbRs = await _imageDrawingService.GenerateThumb(dto);
                    if (thumbRs.Success)
                    {
                        thumb = thumbRs.Value;
                    }

                    if (@override == true)
                    {
                        var sameNameFile =
                            _fileRepository
                            .Repository
                            .Where(f => f.FK_FOLDER == folderId && f.FILENAME.Equals(dto.Filename) == true)
                            .FirstOrDefault();

                        if (sameNameFile != null)
                        {
                            sameNameFile.CONTENT = content;
                            sameNameFile.THUMBNAIL = thumb;
                            sameNameFile.LASTUPDATE_DATE = DateTime.Now;
                            sameNameFile.LASTUPDATE_USER = user;

                            await _fileRepository.Update(sameNameFile);
                        }
                        else
                        {
                            var file = new File()
                            {
                                CONTENT = content,
                                CREATION_DATE = DateTime.Now,
                                CREATION_USER = user,
                                FILENAME = dto.Filename,
                                FK_FOLDER = folderId,
                                //FOLDER = folder,
                                FK_WEBSITE = website,
                                THUMBNAIL = thumb,
                                LASTUPDATE_DATE = DateTime.Now,
                                LASTUPDATE_USER = user,
                            };

                            await _fileRepository.Create(file);
                        }
                    }
                    else
                    {
                        var newfilename = CheckFileToFolder(dto.Filename, folder);
                        var file = new File()
                        {
                            CONTENT = content,
                            CREATION_DATE = DateTime.Now,
                            CREATION_USER = user,
                            FILENAME = newfilename,
                            FK_FOLDER = folderId,
                            //FOLDER = folder,
                            FK_WEBSITE = website,
                            THUMBNAIL = thumb,
                            LASTUPDATE_DATE = DateTime.Now,
                            LASTUPDATE_USER = user,
                        };

                        await _fileRepository.Create(file);
                    }

                    return OperationResult.MakeSuccess();
                }
                else
                {
                    return OperationResult<string>.MakeFailure(new[] { ErrorMessage.Create("ADDFILE_TO_FOLDER", "FOLDER_NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ADDFILE_TO_FOLDER", ex);
                return OperationResult<string>.MakeFailure(new[] { ErrorMessage.Create("ADDFILE_TO_FOLDER", "GENERIC_ERROR") });
            }
        }

        private string CheckFileToFolder(string filename, Folder folder)
        {
            File sameNameFile = null;
            var count = 0;
            do
            {
                var filenameParts = filename.Split('.', StringSplitOptions.RemoveEmptyEntries);
                string newFilename = $"{filenameParts[0]}({count}).{filenameParts[1]}";

                sameNameFile = _fileRepository
                    .Repository
                    .Where(f => f.FK_FOLDER == folder.ID && (count == 0 ? f.FILENAME.Equals(filename) : f.FILENAME.Equals(newFilename)))
                    .SingleOrDefault();

                if (sameNameFile == null)
                {
                    filename = count == 0 ? filename : newFilename;
                }
                else
                {
                    count++;
                }
            } while (sameNameFile != null);

            return filename;
        }

        public async Task<OperationResult<FileDTO>> PasteFile(string user, int newFolder, Guid fileId, bool move)
        {
            try
            {
                var file = await _fileRepository
                    .Read(fileId);

                if (file != null)
                {
                    var folder = await _folderRepository
                        .Read(newFolder);

                    if (folder != null)
                    {
                        var newFilename = CheckFileToFolder(file.FILENAME, folder);

                        var fileCopy = new File()
                        {
                            CONTENT = file.CONTENT,
                            CREATION_DATE = move ? file.CREATION_DATE : DateTime.Now,
                            LASTUPDATE_DATE = DateTime.Now,
                            FILENAME = newFilename,
                            FK_FOLDER = folder.ID,
                            THUMBNAIL = file.THUMBNAIL,
                            FK_WEBSITE = folder.FK_WEBSITE,
                            CREATION_USER = move ? file.CREATION_USER : user,
                            LASTUPDATE_USER = user,
                        };

                        var newFile = await _fileRepository.Create(fileCopy);

                        if (move)
                        {
                            await _fileRepository.Delete(file);
                        }

                        var dto = _mapper.Map<FileDTO>(newFile);

                        return OperationResult<FileDTO>.MakeSuccess(dto);
                    }
                    else
                    {
                        return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("PASTEFILE", "FILE_NOT_FOUND") });
                    }
                }
                else
                {
                    return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("PASTEFILE", "FOLDER_NOT_FOUND") });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("PASTEFILE", ex);
                return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("PASTEFILE", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult<FileDTO>> MoveFile(string user, int newFolder, Guid fileId)
        {
            return await PasteFile(user, newFolder, fileId, true);
        }

        public async Task<OperationResult<FolderDTO>> GetAllFoldersByWebsite(string website)
        {
            try
            {
                var websites = new[] { website };

                var folderRs = await GetAllFolders(websites);

                if (folderRs.Success)
                {
                    var folder = folderRs.Value.FirstOrDefault();

                    return OperationResult<FolderDTO>.MakeSuccess(folder);
                }
                else
                {
                    return OperationResult<FolderDTO>.MakeFailure(folderRs.Errors);
                }

                //var primaryFolder =
                //    _folderRepository
                //    .Repository
                //    .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == null)
                //    .Select(s => new Folder()
                //    {
                //        FK_PARENT = s.FK_PARENT,
                //        FK_WEBSITE = s.FK_WEBSITE,
                //        ID = s.ID,
                //        NAME = s.NAME,
                //        NOTE = s.NOTE,
                //    })
                //    .FirstOrDefault();

                //if (primaryFolder != null)
                //{
                //    var dto = _mapper.Map<FolderDTO>(primaryFolder);
                //    dto.SubFolders = await GetAllSubfoldersByFolder(primaryFolder.ID);

                //    return OperationResult<FolderDTO>.MakeSuccess(dto);
                //}
                //else
                //{
                //    var dto = new FolderDTO()
                //    {
                //        Id = 0,
                //        Name = website,
                //        SubFolders = new List<FolderDTO>(),
                //        Website = website,
                //    };

                //    return OperationResult<FolderDTO>.MakeSuccess(dto);
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError("GETALLFOLDERSBYWEBSITE", ex);
                return OperationResult<FolderDTO>.MakeFailure(new[] { ErrorMessage.Create("GETALLFOLDERSBYWEBSITE", "GENERIC_ERROR") });
            }
        }

        private async Task<IList<FolderDTO>> GetAllSubfoldersByFolder(int parentFolder)
        {
            var folders =
                    _folderRepository
                    .Repository
                    .Where(f => f.FK_PARENT == parentFolder)
                    .Select(s => new Folder()
                    {
                        FK_PARENT = s.FK_PARENT,
                        FK_WEBSITE = s.FK_WEBSITE,
                        ID = s.ID,
                        NAME = s.NAME,
                        NOTE = s.NOTE,
                    })
                    .OrderBy(s => s.NAME)
                    .ToList();

            var list = new List<FolderDTO>();
            foreach (var f in folders)
            {
                var dto = _mapper.Map<FolderDTO>(f);
                dto.SubFolders = await GetAllSubfoldersByFolder(f.ID);

                list.Add(dto);
            }
            return list;
        }


        public async Task<OperationResult> CreateDefaultWebsiteFolders(string website)
        {
            try
            {
                var exist = _folderRepository
                    .Repository
                    .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == null)
                    .Any();

                if (!exist)
                {
                    await _folderRepository.BeginTransaction();

                    var parent = await CreateFolderByName(website, website, null);
                    var files = await CreateFolderByName(website, "Files", parent);
                    var images = await CreateFolderByName(website, "Images", parent);
                    var filescommons = await CreateFolderByName(website, "Global", files);
                    var imagescommons = await CreateFolderByName(website, "Global", images);

                    await _folderRepository.CommitTransaction();

                    return OperationResult.MakeSuccess();
                }
                else
                {
                    return OperationResult.MakeFailure(new[] { ErrorMessage.Create("CREATEDEFAULTFOLDERS", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                await _folderRepository.RollbackTransaction();
                _logger.LogError("GETALLFOLDERSBYWEBSITE", ex);
                return OperationResult.MakeFailure(new[] { ErrorMessage.Create("CREATEDEFAULTFOLDERS", "GENERIC_ERROR") });
            }
        }

        public async Task<OperationResult> CreateDefaultInstanceFolders(string website, string site, string[] languages)
        {
            try
            {
                var primaryFolder = _folderRepository
                    .Repository
                    .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == null && f.NAME == website)
                    .FirstOrDefault();

                if (primaryFolder == null)
                {
                    var defaultRs = await CreateDefaultWebsiteFolders(website);

                    if (defaultRs.Success == false)
                    {
                        return defaultRs;
                    }

                    primaryFolder = _folderRepository
                        .Repository
                        .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == null && f.NAME == website)
                        .FirstOrDefault();
                }


                await _folderRepository.BeginTransaction();

                var files =
                    _folderRepository
                        .Repository
                        .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == primaryFolder.ID && f.NAME.Equals("Files", StringComparison.InvariantCultureIgnoreCase))
                        .SingleOrDefault();

                if (files == null)
                {
                    files = await CreateFolderByName(website, "Files", primaryFolder);
                }



                var images =
                    _folderRepository
                        .Repository
                        .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == primaryFolder.ID && f.NAME.Equals("Images", StringComparison.InvariantCultureIgnoreCase))
                        .SingleOrDefault();
                if (images == null)
                {
                    images = await CreateFolderByName(website, "Images", primaryFolder);
                }

                var instanceFileFolder = 
                    _folderRepository
                        .Repository
                        .Where(c => c.FK_WEBSITE == website && c.FK_PARENT != null && c.FK_PARENT == files.ID && c.NAME.Equals(site, StringComparison.InvariantCultureIgnoreCase))
                        .SingleOrDefault();

                await CreateLanguagesFolder(website, site, instanceFileFolder, files, languages);

                var instanceImageFolder = 
                    _folderRepository
                        .Repository
                   .Where(c => c.FK_WEBSITE == website && c.FK_PARENT != null && c.FK_PARENT == images.ID && c.NAME.Equals(site, StringComparison.InvariantCultureIgnoreCase))
                   .SingleOrDefault();

                await CreateLanguagesFolder(website, site, instanceImageFolder, images, languages);


                await _folderRepository.CommitTransaction();

                return OperationResult.MakeSuccess();

            }
            catch (Exception ex)
            {
                await _folderRepository.RollbackTransaction();
                _logger.LogError("GETALLFOLDERSBYWEBSITE", ex);
                return OperationResult.MakeFailure(new[] { ErrorMessage.Create("CREATEDEFAULTFOLDERS", "GENERIC_ERROR") });
            }
        }

        private async Task CreateLanguagesFolder(string website, string folderName, Folder folder, Folder parent, string[] languages)
        {
            if (folder == null)
            {
                folder = await CreateFolderByName(website, folderName, parent);
            }

            if (languages != null && languages.Length > 0)
            {
                foreach (var l in languages)
                {
                    await CreateFolderByName(website, l, parent);
                }
            }
        }

        private async Task<Folder> CreateFolderByName(string website, string name, Folder parent)
        {
            int? parentId = parent != null ? (int?)parent.ID : null;
            var exfolder =
                _folderRepository
                .Repository
                    .Where(c => c.FK_WEBSITE == website && c.FK_PARENT == parentId && c.NAME == name)
                    .FirstOrDefault();

            if (exfolder == null)
            {
                var folder = new Folder();
                folder.DELETABLE = false;
                folder.NAME = name;
                folder.NOTE = $"System folder {name}";
                folder.FK_WEBSITE = website;
                folder.FK_PARENT = parent.ID;

                var newFolder = await _folderRepository.Create(folder);

                return newFolder;
            }
            else
            {
                return exfolder;
            }
        }

        public async Task<OperationResult<FileDTO>> GetFileByFullname(string fullFilename)
        {
            try
            {
                var fileName = fullFilename.Replace("@/", "").Split('/', StringSplitOptions.RemoveEmptyEntries).Last();

                var files = _fileRepository
                    .Repository
                    .Where(f => f.FILENAME.Equals(fileName))
                    .Select(s => new File()
                    {
                        ID = s.ID,
                        CONTENT = s.CONTENT,
                    })
                    .ToList();

                if (files.Count > 0)
                {
                    if (files.Count == 1)
                    {
                        var file = files.Single();

                        var dto = new FileDTO()
                        {
                            Id = file.ID,
                            Content = file.CONTENT,
                        };

                        return await Task.FromResult(OperationResult<FileDTO>.MakeSuccess(dto));
                    }
                    else
                    {
                        var foldersName = fullFilename.Replace(fileName, "").Replace("@/", "").Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        throw new NotImplementedException();
                        //var filesFolder =
                        //    files
                        //    .Select(f => f.FOLDER)
                        //    .ToList();

                        //Folder folderFound = null;
                        //int folderIndex = foldersName.Length - 1;
                        //do
                        //{
                            //var currentFolder = foldersName[folderIndex];

                            //var folders =
                            //    filesFolder
                            //    .Where(f => f.NAME.Equals(currentFolder, StringComparison.InvariantCultureIgnoreCase))
                            //    .ToList();

                            //if (folders.Count == 1)
                            //{
                            //    folderFound = folders.Single();
                            //}
                            //else
                            //{
                            //    filesFolder = folders
                            //        .Where(f => f.FK_PARENT != null)
                            //        .Select(f => f.FK_PARENT)
                            //        .ToList();

                            //    folderIndex--;
                            //}
                            throw new NotImplementedException();

                        //} while (folderFound == null && folderIndex >= 0);

                        //if (folderFound == null || folderIndex < 0)
                        //{
                        //    return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("GETFILEBYFULLNAME", "FOLDER_NOT_FOUND") }); ;
                        //}
                        //else
                        //{
                            //Folder fileFolder = folderFound;

                            //try
                            //{
                            //    for (int i = folderIndex; i < foldersName.Length - 1; i++)
                            //    {
                            //        var subFoldername = foldersName[i + 1];
                            //        fileFolder =
                            //            fileFolder.SUBFOLDERS
                            //            .Where(f => f.NAME.Equals(subFoldername, StringComparison.InvariantCultureIgnoreCase))
                            //            .Single();
                            //    }

                            //    var file = fileFolder
                            //        .FILES
                            //        .Where(f => f.FILENAME.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                            //        .Single();

                            //    var dto = new FileDTO()
                            //    {
                            //        Id = file.ID,
                            //        Content = file.CONTENT,
                            //    };

                            //    return OperationResult<FileDTO>.MakeSuccess(dto);
                            //}
                            //catch
                            //{
                            //    return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("GETFILEBYFULLNAME", "GENERIC_ERROR") });
                            //}
                            throw new NotImplementedException();
                        //}
                    }
                }
                else
                {
                    return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("GETFILEBYFULLNAME", "GENERIC_ERROR") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GETFILEBYFULLNAME", ex);
                return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("GETFILEBYFULLNAME", "GENERIC_ERROR") });
            }
        }

        //public void RenameFolder(string oldName, string newName, bool isVendor, MorganEntities context)
        //{
        //    var folders =
        //        context.Cartelle
        //        .Where(c => c.NAME.Equals(oldName) && c.ELIMINABILE == false && c.PARENT_FOLDER.HasValue == !isVendor);

        //    if (isVendor)
        //    {
        //        var vendorFolder = folders.Single();
        //        vendorFolder.NAME = newName;
        //    }
        //    else
        //    {
        //        foreach (var folder in folders.ToList())
        //        {
        //            folder.NAME = newName;
        //        }
        //    }
        //}

        public async Task<OperationResult<FileDTO>> RenameFile(Guid fileId, string newName, string user)
        {
            try
            {
                var entity = await _fileRepository
                    .Read(fileId);

                if (entity != null)
                {
                    entity.FILENAME = newName;
                    entity.LASTUPDATE_DATE = DateTime.Now;
                    entity.LASTUPDATE_USER = user;

                    var result = await _fileRepository.Update(entity);

                    var r = _mapper.Map<FileDTO>(result);

                    return OperationResult<FileDTO>.MakeSuccess(r);
                }
                else
                {
                    return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "NOT_FOUND") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SAVE", ex);
                return OperationResult<FileDTO>.MakeFailure(new[] { ErrorMessage.Create("SAVE", "GENERIC_ERROR") });
            }
        }

        //public IList<FileDto> GetLastFiles(int count, string[] userVendor)
        //{
        //    using (MorganEntities context = new MorganEntities())
        //    {
        //        IList<File> files =
        //            context
        //            .Files
        //            .Where(f => userVendor.Contains(f.FK_VENDOR))
        //            .OrderByDescending(f => f.DATA_ULTIMA_MODIFICA)
        //            .Take(count)
        //            .ToList();

        //        List<FileDto> list = new List<FileDto>();

        //        foreach (var f in files)
        //        {
        //            list.Add(
        //                new FileDto()
        //                {
        //                    DataUltimaModifica = f.DATA_ULTIMA_MODIFICA.ToString(),
        //                    Filename = f.FILENAME,
        //                    UniqueIdentifier = f.ID,
        //                    HasThumbnail = f.THUMBNAIL != null,
        //                });
        //        }

        //        return list;
        //    }
        //}
    }
}
