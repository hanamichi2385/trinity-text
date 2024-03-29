﻿namespace TrinityText.Business
{
    public class TextTypeDTO
    {
        public int? Id { get; set; }

        public string Name { get; set; }

        public string Note { get; set; }

        public string Subfolder { get; set; }

        public bool HasSubfolder
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Subfolder);
            }
        }

        public int TextNumbers { get; set; }
    }
}