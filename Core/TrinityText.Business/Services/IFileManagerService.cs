using Resulz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IFileManagerService
    {
        Task<OperationResult> AddFile(string user, string website, int folderId, FileDTO dto, bool @override, bool useOriginal);
        Task<OperationResult> CreateDefaultInstanceFolders(string website, string site, string[] languages);
        Task<OperationResult> CreateDefaultWebsiteFolders(string website);
        Task<OperationResult> DeleteFile(Guid id);
        Task<OperationResult<IReadOnlyCollection<FolderDTO>>> GetAllFolders(string[] websites);
        Task<OperationResult<FolderDTO>> GetAllFoldersByWebsite(string website);
        Task<OperationResult<FileDTO>> GetFile(Guid id, bool withThumb);
        Task<OperationResult<FileDTO>> GetFileByFullname(string fullFilename);
        Task<OperationResult<string>> GetFileLink(Guid id);
        Task<OperationResult<IReadOnlyCollection<FileDTO>>> GetFilesByFolder(string website, int id, bool withFileContent, DateTime? lastUpdate);
        Task<OperationResult<FolderDTO>> GetFolder(int id);
        Task<OperationResult<FileDTO>> MoveFile(string user, int newFolder, Guid fileId);
        Task<OperationResult<FileDTO>> PasteFile(string user, int newFolder, Guid fileId, bool move);
        Task<OperationResult> RemoveFolder(int id);
        Task<OperationResult<FileDTO>> RenameFile(Guid fileId, string newName, string user);
        Task<OperationResult<FolderDTO>> SaveFolder(int? parentFolderId, FolderDTO dto);
    }
}