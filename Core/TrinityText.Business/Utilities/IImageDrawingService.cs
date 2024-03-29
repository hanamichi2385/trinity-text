﻿using Resulz;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IImageDrawingService
    {
        Task<OperationResult<byte[]>> GenerateThumb(FileDTO dto);

        Task<OperationResult<byte[]>> Compression(FileDTO dto);
    }
}
