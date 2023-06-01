using Ganss.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities.Excel
{
    public class ExcelMapperService : IExcelService
    {
        public Task<byte[]> GetExcelFileStream(PageDTO[] list)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetExcelFileStream(WidgetDTO[] list)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetExcelFileStream(TextDTO[] list)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetExcelFileStream(IDictionary<KeyValuePair<string, string>, TextDTO[]> resourcesPerInstanceLang)
        {
            throw new NotImplementedException();
        }

        public Task<TextDTO[]> GetTextsFromStream(string user, Stream fileStream)
        {
            var em = new ExcelMapper(fileStream);
        }
    }
}
