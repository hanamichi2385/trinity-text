using AutoMapper;
using Microsoft.Extensions.Logging;
using Resulz;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public async Task<OperationResult<IReadOnlyCollection<FolderDTO>>> GetAllFolders(string[] websites)
        {
            try
            {
                var dtos = await _folderRepository.ToListAsync(
                    _folderRepository
                        .Repository
                        .Where(f => websites.Contains(f.FK_WEBSITE))
                        .Select(s => new FolderDTO
                        {
                            Id = s.ID,
                            ParentId = s.FK_PARENT,
                            Website = s.FK_WEBSITE,
                            Name = s.NAME,
                            Note = s.NOTE,
                        }));

                var result = BuildFolderTree(dtos);

                return OperationResult<IReadOnlyCollection<FolderDTO>>.MakeSuccess(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETALL {message}", ex.Message);
                return OperationResult<IReadOnlyCollection<FolderDTO>>.MakeFailure([ErrorMessage.Create("GET_ALL", "GENERIC_ERROR")]);
            }
        }

        private static IReadOnlyCollection<FolderDTO> BuildFolderTree(List<FolderDTO> folders)
        {
            var byParent = folders.ToLookup(f => f.ParentId);

            foreach (var f in folders)
            {
                f.SubFolders = byParent[f.Id].ToList();
            }

            return byParent[null].ToList();
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
                    return OperationResult<FolderDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<FolderDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        private static readonly char[] InvalidNameChars = System.IO.Path.GetInvalidFileNameChars();

        private static string NormalizeFilename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Filename cannot be empty");

            name = name.Trim();

            if (name.IndexOfAny(InvalidNameChars) >= 0)
                throw new ArgumentException($"Invalid char in name: {name}");

            // rejects ".", ".." and separators: file names end up in export paths
            PathSafety.EnsureValidSegment(name, "file name");

            return name;
        }

        private static string NormalizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name cannot be empty");
            name = name.Trim();
            PathSafety.EnsureValidSegment(name, "folder name");
            return name;
        }

        /// <summary>
        /// The parent must exist in the same website and must not be the folder itself or one of its descendants
        /// (a cycle would make every tree walk loop forever).
        /// </summary>
        private async Task<bool> IsValidParent(int? folderId, int? parentFolderId, string website)
        {
            if (parentFolderId == null)
            {
                return true;
            }

            if (folderId.HasValue && parentFolderId == folderId)
            {
                return false;
            }

            var websiteFolders = await _folderRepository.ToListAsync(
                _folderRepository
                    .Repository
                    .Where(f => f.FK_WEBSITE == website)
                    .Select(f => new { f.ID, f.FK_PARENT }));

            var parents = websiteFolders.ToDictionary(f => f.ID, f => f.FK_PARENT);
            if (!parents.ContainsKey(parentFolderId.Value))
            {
                return false; // missing or belonging to another website
            }

            // walk up from the new parent: reaching the folder being edited means it would become its own ancestor
            var visited = new HashSet<int>();
            int? current = parentFolderId;
            while (current != null && visited.Add(current.Value))
            {
                if (folderId.HasValue && current == folderId)
                {
                    return false;
                }

                current = parents.TryGetValue(current.Value, out var next) ? next : null;
            }

            return true;
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
                        if (!await IsValidParent(entity.ID, parentFolderId, dto.Website ?? entity.FK_WEBSITE))
                        {
                            return OperationResult<FolderDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_PARENT")]);
                        }

                        entity.NAME = NormalizeFolderName(dto.Name);
                        entity.FK_PARENT = parentFolderId;
                        entity.NOTE = dto.Note;
                        entity.FK_WEBSITE = dto.Website;

                        var result = await _folderRepository.Update(entity);

                        var r = _mapper.Map<FolderDTO>(result);

                        return OperationResult<FolderDTO>.MakeSuccess(r);
                    }
                    else
                    {
                        return OperationResult<FolderDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                    }
                }
                else
                {
                    dto.Name = NormalizeFolderName(dto.Name);

                    if (!await IsValidParent(null, parentFolderId, dto.Website))
                    {
                        return OperationResult<FolderDTO>.MakeFailure([ErrorMessage.Create("SAVE", "INVALID_PARENT")]);
                    }

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
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<FolderDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> RemoveFolder(int id)
        {
            try
            {
                var entity = await _folderRepository
                    .Read(id);

                if (entity == null)
                {
                    return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_FOUND")]);
                }

                if (!entity.DELETABLE)
                {
                    return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_DELETABLE")]);
                }

                // A deletable folder may still contain system (non-deletable) folders in its subtree;
                // deleting the parent would cascade-remove them, bypassing the DELETABLE flag.
                if (await HasNonDeletableDescendant(entity))
                {
                    return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_DELETABLE")]);
                }

                await _folderRepository.BeginTransaction();

                await EmptyFolder(entity);

                await _folderRepository.CommitTransaction();

                return OperationResult.MakeSuccess();
            }
            catch (Exception ex)
            {
                await _folderRepository.RollbackTransaction();
                _logger.LogError(ex, "REMOVE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "GENERIC_ERROR")]);
            }
        }

        private async Task<bool> HasNonDeletableDescendant(Folder root)
        {
            var websiteFolders = await _folderRepository.ToListAsync(
                _folderRepository
                    .Repository
                    .Where(f => f.FK_WEBSITE == root.FK_WEBSITE)
                    .Select(f => new Folder { ID = f.ID, FK_PARENT = f.FK_PARENT, DELETABLE = f.DELETABLE }));

            var byParent = websiteFolders.ToLookup(f => f.FK_PARENT);

            var stack = new Stack<int>();
            stack.Push(root.ID);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                foreach (var child in byParent[current])
                {
                    if (!child.DELETABLE)
                    {
                        return true;
                    }
                    stack.Push(child.ID);
                }
            }
            return false;
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
                    var result = _mapper.Map<FileDTO>(entity);
                    if (withThumb)
                    {
                        result.Content = entity.THUMBNAIL;
                    }

                    return OperationResult<FileDTO>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("GET", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<IReadOnlyCollection<FileDTO>>> GetFilesByFolder(string website, int id, bool withFileContent, DateTime? lastUpdate)
        {
            try
            {
                var query = _fileRepository
                    .Repository
                    .Where(f => f.FK_WEBSITE == website && f.FK_FOLDER == id);

                if (lastUpdate.HasValue)
                {
                    var fromDate = lastUpdate.Value.Date;
                    query = query.Where(f => f.LASTUPDATE_DATE >= fromDate);
                }

                var list = await _fileRepository.ToListAsync(
                    query
                        .OrderBy(f => f.FILENAME)
                        .Select(f => new FileDTO
                        {
                            Id = f.ID,
                            Filename = f.FILENAME,
                            CreationDate = f.CREATION_DATE,
                            CreationUser = f.CREATION_USER,
                            LastUpdate = f.LASTUPDATE_DATE,
                            LastUpdateUser = f.LASTUPDATE_USER,
                            Content = withFileContent ? f.CONTENT : null,
                            HasThumbnail = f.THUMBNAIL != null,
                        }));

                return OperationResult<IReadOnlyCollection<FileDTO>>.MakeSuccess(list.AsReadOnly());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {message}", ex.Message);
                return OperationResult<IReadOnlyCollection<FileDTO>>.MakeFailure([ErrorMessage.Create("GETFILES_BYFOLDER", "GENERIC_ERROR")]);
            }
        }



        public async Task<OperationResult> DeleteFile(Guid id)
        {
            try
            {
                // set-based delete: the file content and thumbnail are never loaded
                var deleted = await _fileRepository.ExecuteDeleteAsync(
                    _fileRepository.Repository.Where(f => f.ID == id));

                return deleted > 0
                    ? OperationResult.MakeSuccess()
                    : OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "NOT_FOUND")]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REMOVE {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("REMOVE", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult<string>> GetFileLink(Guid id)
        {
            try
            {
                // metadata only: CONTENT / THUMBNAIL are not needed to build the link
                var entity = await _fileRepository.FirstOrDefaultAsync(
                    _fileRepository
                        .Repository
                        .Where(f => f.ID == id)
                        .Select(f => new { f.FILENAME, f.FK_FOLDER, f.FK_WEBSITE }));
                if (entity != null)
                {
                    var folderMap = (await _folderRepository.ToListAsync(
                        _folderRepository
                            .Repository
                            .Where(f => f.FK_WEBSITE == entity.FK_WEBSITE)
                            .Select(f => new { f.ID, f.NAME, f.FK_PARENT })))
                        .ToDictionary(f => f.ID);

                    var segments = new Stack<string>();
                    segments.Push(Uri.EscapeDataString(entity.FILENAME));

                    var visited = new HashSet<int>();
                    var currentFolderId = (int?)entity.FK_FOLDER;
                    while (currentFolderId != null && visited.Add(currentFolderId.Value) && folderMap.TryGetValue(currentFolderId.Value, out var folder))
                    {
                        segments.Push(Uri.EscapeDataString(folder.NAME));
                        currentFolderId = folder.FK_PARENT;
                    }

                    var result = $"@/{string.Join("/", segments)}";
                    return OperationResult<string>.MakeSuccess(result);
                }
                else
                {
                    return OperationResult<string>.MakeFailure([ErrorMessage.Create("GET FILE LINK", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET FILE LINK {message}", ex.Message);
                return OperationResult<string>.MakeFailure([ErrorMessage.Create("GET FILE LINK", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> AddFile(string user, string website, int folderId, FileDTO dto, bool @override, bool useOriginal)
        {
            try
            {
                var folder = await _folderRepository
                    .Read(folderId);

                if (folder != null)
                {
                    dto.Filename = NormalizeFilename(dto.Filename);

                    var content = dto.Content;

                    if (useOriginal == false)
                    {
                        var compressionRs = await _imageDrawingService.Compression(dto);
                        if (compressionRs.Success)
                        {
                            content = compressionRs.Value;
                        }
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
                    return OperationResult<string>.MakeFailure([ErrorMessage.Create("ADDFILE_TO_FOLDER", "FOLDER_NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ADDFILE_TO_FOLDEER {message}", ex.Message);
                return OperationResult<string>.MakeFailure([ErrorMessage.Create("ADDFILE_TO_FOLDER", "GENERIC_ERROR")]);
            }
        }

        private string CheckFileToFolder(string filename, Folder folder)
        {
            var existingNames = _fileRepository
                .Repository
                .Where(f => f.FK_FOLDER == folder.ID)
                .Select(f => f.FILENAME)
                .ToList()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!existingNames.Contains(filename))
                return filename;

            var lastDot = filename.LastIndexOf('.');
            var baseName = lastDot >= 0 ? filename[..lastDot] : filename;
            var ext = lastDot >= 0 ? filename[lastDot..] : string.Empty;
            var count = 1;
            string candidate;
            do
            {
                candidate = $"{baseName}({count++}){ext}";
            } while (existingNames.Contains(candidate));

            return candidate;
        }

        public async Task<OperationResult<FileDTO>> PasteFile(string user, int newFolder, Guid fileId, bool move)
        {
            try
            {
                if (move)
                {
                    return await MoveFileToFolder(user, newFolder, fileId);
                }

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
                            CREATION_DATE = DateTime.Now,
                            LASTUPDATE_DATE = DateTime.Now,
                            FILENAME = newFilename,
                            FK_FOLDER = folder.ID,
                            THUMBNAIL = file.THUMBNAIL,
                            FK_WEBSITE = folder.FK_WEBSITE,
                            CREATION_USER = user,
                            LASTUPDATE_USER = user,
                        };

                        var newFile = await _fileRepository.Create(fileCopy);

                        var dto = _mapper.Map<FileDTO>(newFile);

                        return OperationResult<FileDTO>.MakeSuccess(dto);
                    }
                    else
                    {
                        return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("PASTEFILE", "FILE_NOT_FOUND")]);
                    }
                }
                else
                {
                    return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("PASTEFILE", "FOLDER_NOT_FOUND")]);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PASTEFILE {message}", ex.Message);
                return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("PASTEFILE", "GENERIC_ERROR")]);
            }
        }

        // A move only changes the folder (and name on conflict): update the row in place instead of copying the blobs
        // into a new row and deleting the old one. The file keeps its id, so existing references stay valid.
        private async Task<OperationResult<FileDTO>> MoveFileToFolder(string user, int newFolder, Guid fileId)
        {
            var file = await _fileRepository.FirstOrDefaultAsync(
                _fileRepository
                    .Repository
                    .Where(f => f.ID == fileId)
                    .Select(f => new { f.FILENAME }));

            if (file == null)
            {
                return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("PASTEFILE", "FOLDER_NOT_FOUND")]);
            }

            var folder = await _folderRepository.Read(newFolder);
            if (folder == null)
            {
                return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("PASTEFILE", "FILE_NOT_FOUND")]);
            }

            var newFilename = CheckFileToFolder(file.FILENAME, folder);
            var now = DateTime.Now;
            var folderId = folder.ID;
            var website = folder.FK_WEBSITE;

            await _fileRepository.ExecuteUpdateAsync(
                _fileRepository.Repository.Where(f => f.ID == fileId),
                set => set
                    .Set(f => f.FILENAME, newFilename)
                    .Set(f => f.FK_FOLDER, folderId)
                    .Set(f => f.FK_WEBSITE, website)
                    .Set(f => f.LASTUPDATE_DATE, now)
                    .Set(f => f.LASTUPDATE_USER, user));

            return OperationResult<FileDTO>.MakeSuccess(await GetFileMetadata(fileId));
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
                _logger.LogError(ex, "GETALLFOLDERSBYWEBSITE {message}", ex.Message);
                return OperationResult<FolderDTO>.MakeFailure([ErrorMessage.Create("GETALLFOLDERSBYWEBSITE", "GENERIC_ERROR")]);
            }
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
                    return OperationResult.MakeFailure([ErrorMessage.Create("CREATEDEFAULTFOLDERS", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                await _folderRepository.RollbackTransaction();
                _logger.LogError(ex, "CREATEDEFAULTFOLDERS {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("CREATEDEFAULTFOLDERS", "GENERIC_ERROR")]);
            }
        }

        public async Task<OperationResult> CreateDefaultInstanceFolders(string website, string site, string[] languages)
        {
            try
            {
                var primaryFolder = await _fileRepository.FirstOrDefaultAsync(
                    _folderRepository
                        .Repository
                        .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == null && f.NAME == website));

                if (primaryFolder == null)
                {
                    var defaultRs = await CreateDefaultWebsiteFolders(website);

                    if (defaultRs.Success == false)
                    {
                        return defaultRs;
                    }

                    primaryFolder = await _fileRepository.FirstOrDefaultAsync(
                        _folderRepository
                            .Repository
                            .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == null && f.NAME == website));
                }

                await _folderRepository.BeginTransaction();

                var primaryId = primaryFolder.ID;
                var directChildren = await _fileRepository.ToListAsync(
                    _folderRepository
                        .Repository
                        .Where(f => f.FK_WEBSITE == website && f.FK_PARENT == primaryId));

                var files = directChildren.FirstOrDefault(f => string.Equals(f.NAME, "Files", StringComparison.OrdinalIgnoreCase))
                            ?? await CreateFolderByName(website, "Files", primaryFolder);

                var images = directChildren.FirstOrDefault(f => string.Equals(f.NAME, "Images", StringComparison.OrdinalIgnoreCase))
                             ?? await CreateFolderByName(website, "Images", primaryFolder);

                var subIds = new[] { files.ID, images.ID };
                var instanceFolders = await _fileRepository.ToListAsync(
                    _folderRepository
                        .Repository
                        .Where(c => c.FK_WEBSITE == website
                            && c.FK_PARENT != null
                            && subIds.Contains(c.FK_PARENT.Value)
                            && c.NAME == site));

                var instanceFileFolder = instanceFolders.FirstOrDefault(f => f.FK_PARENT == files.ID);
                var instanceImageFolder = instanceFolders.FirstOrDefault(f => f.FK_PARENT == images.ID);

                await CreateLanguagesFolder(website, site, instanceFileFolder, files, languages);
                await CreateLanguagesFolder(website, site, instanceImageFolder, images, languages);

                await _folderRepository.CommitTransaction();

                return OperationResult.MakeSuccess();

            }
            catch (Exception ex)
            {
                await _folderRepository.RollbackTransaction();
                _logger.LogError(ex, "CREATEDEFAULTFOLDERS {message}", ex.Message);
                return OperationResult.MakeFailure([ErrorMessage.Create("CREATEDEFAULTFOLDERS", "GENERIC_ERROR")]);
            }
        }

        private async Task CreateLanguagesFolder(string website, string folderName, Folder folder, Folder parent, string[] languages)
        {
            if (folder == null)
            {
                await CreateFolderByName(website, folderName, parent);
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
            // site / language names become directories in the export
            PathSafety.EnsureValidSegment(name, "folder name");

            int? parentId = parent != null ? (int?)parent.ID : null;
            var exfolder =
                _folderRepository
                .Repository
                    .Where(c => c.FK_WEBSITE == website && c.FK_PARENT == parentId && c.NAME == name)
                    .FirstOrDefault();

            if (exfolder == null)
            {
                var folder = new Folder()
                {
                    DELETABLE = false,
                    NAME = name,
                    NOTE = $"System folder {name}",
                    FK_WEBSITE = website,
                    FK_PARENT = parentId,
                };

                var newFolder = await _folderRepository.Create(folder);

                return newFolder;
            }
            else
            {
                return exfolder;
            }
        }

        public Task<OperationResult<FileDTO>> GetFileByFullname(string fullFilename)
            => ResolveFileByFullname(fullFilename, withContent: true);

        public async Task<OperationResult<Guid>> GetFileIdByFullname(string fullFilename)
        {
            var rs = await ResolveFileByFullname(fullFilename, withContent: false);
            return rs.Success
                ? OperationResult<Guid>.MakeSuccess(rs.Value.Id)
                : OperationResult<Guid>.MakeFailure(rs.Errors);
        }

        private async Task<OperationResult<FileDTO>> ResolveFileByFullname(string fullFilename, bool withContent)
        {
            try
            {
                // Path format: @/<website>/<folder1>/.../<filename>
                var parts = fullFilename.Replace("@/", string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("GETFILEBYFULLNAME", "NOT_FOUND")]);
                }

                var fileName = parts[^1];

                // the content column is projected only when the caller needs it
                var files = withContent
                    ? await _fileRepository.ToListAsync(
                        _fileRepository
                            .Repository
                            .Where(f => f.FILENAME.Equals(fileName))
                            .Select(s => new File()
                            {
                                ID = s.ID,
                                CONTENT = s.CONTENT,
                                FK_FOLDER = s.FK_FOLDER,
                                FK_WEBSITE = s.FK_WEBSITE,
                            }))
                    : await _fileRepository.ToListAsync(
                        _fileRepository
                            .Repository
                            .Where(f => f.FILENAME.Equals(fileName))
                            .Select(s => new File()
                            {
                                ID = s.ID,
                                FK_FOLDER = s.FK_FOLDER,
                                FK_WEBSITE = s.FK_WEBSITE,
                            }));

                if (files.Count == 0)
                {
                    return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("GETFILEBYFULLNAME", "NOT_FOUND")]);
                }

                if (files.Count == 1)
                {
                    // Single match: behaviour unchanged (cross-tenant resolution tracked separately as an open point).
                    var file = files[0];
                    return OperationResult<FileDTO>.MakeSuccess(new FileDTO { Id = file.ID, Content = file.CONTENT });
                }

                // Multiple files share the filename: disambiguate by the folder path encoded in the link.
                // The full path minus the filename is the folder chain (root → leaf), whose root folder is named after the website.
                var website = parts[0];
                var expectedChain = parts[..^1];

                var folderMap = (await _folderRepository.ToListAsync(
                    _folderRepository
                        .Repository
                        .Where(f => f.FK_WEBSITE == website)
                        .Select(f => new Folder { ID = f.ID, NAME = f.NAME, FK_PARENT = f.FK_PARENT })))
                    .ToDictionary(f => f.ID);

                foreach (var candidate in files)
                {
                    if (FolderChainMatches(candidate.FK_FOLDER, expectedChain, folderMap))
                    {
                        return OperationResult<FileDTO>.MakeSuccess(new FileDTO { Id = candidate.ID, Content = candidate.CONTENT });
                    }
                }

                return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("GETFILEBYFULLNAME", "FOLDER_NOT_FOUND")]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GETFILEBYFULLNAME {message}", ex.Message);
                return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("GETFILEBYFULLNAME", "GENERIC_ERROR")]);
            }
        }

        private static bool FolderChainMatches(int folderId, string[] expectedChain, Dictionary<int, Folder> folderMap)
        {
            // Reconstruct the folder names from the file's folder up to the root, then compare leaf→root
            // against the expected chain read in reverse.
            var index = expectedChain.Length - 1;
            int? currentId = folderId;

            while (currentId != null && folderMap.TryGetValue(currentId.Value, out var folder))
            {
                if (index < 0 || !string.Equals(folder.NAME, expectedChain[index], StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
                index--;
                currentId = folder.FK_PARENT;
            }

            // Matched iff every expected segment was consumed and we reached a root folder.
            return index < 0 && currentId == null;
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
                var name = NormalizeFilename(newName);
                var now = DateTime.Now;

                // targeted UPDATE: CONTENT / THUMBNAIL are neither loaded nor rewritten
                var updated = await _fileRepository.ExecuteUpdateAsync(
                    _fileRepository.Repository.Where(f => f.ID == fileId),
                    set => set
                        .Set(f => f.FILENAME, name)
                        .Set(f => f.LASTUPDATE_DATE, now)
                        .Set(f => f.LASTUPDATE_USER, user));

                if (updated > 0)
                {
                    var r = await GetFileMetadata(fileId);

                    return OperationResult<FileDTO>.MakeSuccess(r);
                }
                else
                {
                    return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("SAVE", "NOT_FOUND")]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAVE {message}", ex.Message);
                return OperationResult<FileDTO>.MakeFailure([ErrorMessage.Create("SAVE", "GENERIC_ERROR")]);
            }
        }

        // FileDTO without content (HasThumbnail is computed in SQL, the thumbnail is not loaded)
        private Task<FileDTO> GetFileMetadata(Guid fileId)
            => _fileRepository.FirstOrDefaultAsync(
                _fileRepository
                    .Repository
                    .Where(f => f.ID == fileId)
                    .Select(f => new FileDTO
                    {
                        Id = f.ID,
                        Filename = f.FILENAME,
                        CreationDate = f.CREATION_DATE,
                        CreationUser = f.CREATION_USER,
                        LastUpdate = f.LASTUPDATE_DATE,
                        LastUpdateUser = f.LASTUPDATE_USER,
                        HasThumbnail = f.THUMBNAIL != null,
                    }));

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
