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
        Created = 0,
        Generating = 50,
        Publishing = 100,
        Success = 999,
    }
}
