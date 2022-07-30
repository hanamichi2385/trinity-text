using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TrinityText.Business
{
    public class TextDTO
    {
        public int? Id { get;set;}

        public string Website { get; set; }

        public string Site { get; set; }

        public string Language { get; set; }

        public string Name { get; set; }

        public TextRevisionDTO TextRevision { get; set; }

        public int? TextTypeId { get; set; }

        public string Country { get; set; }

        public TextTypeDTO TextType { get; set; }

        public bool Active { get; set; }
    }
}