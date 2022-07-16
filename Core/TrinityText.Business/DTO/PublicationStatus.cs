using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public enum PublicationStatus
    {
        Failed = -1,
        Started = 0,
        Generated = 50,
        Publishing = 100,
        Success = 999,
    }
}
