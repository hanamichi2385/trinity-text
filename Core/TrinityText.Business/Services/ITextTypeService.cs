using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ITextTypeService
    {
        Task<IList<TextTypeDTO>> GetAll();
    }
}
