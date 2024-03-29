﻿using Resulz;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ITransferService
    {
        string Key { get; }
        Task<string> Upload(string tenant, string website, DirectoryInfo baseDirectory, string host, string username, string password, string path);
        Task<byte[]> GetFile(string tenant, string website, string file, string host, string username, string password, string path);
    }

    public interface ITransferServiceCoordinator 
    {
        IDictionary<string, ITransferService> Services { get; }

        Task<OperationResult> Upload(string tenant, string website, DirectoryInfo baseDirectory, string host, string username, string password);
        Task<OperationResult<byte[]>> GetFile(string tenant, string website, string file, string host, string username, string password);
    }
}
