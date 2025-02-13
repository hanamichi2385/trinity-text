﻿using Resulz;
using System.Collections.Generic;

namespace TrinityText.Business.Schema
{
    public class GalleryAtom : BaseAtom<GalleryAtom>
    {
        public GalleryAtom() : base(AtomType.Gallery)
        {
            Items = [];
        }

        public List<ImageParticol> Items { get; set; }
        public string ItemName { get; set; }

        public string PathName { get; set; }

        public string LinkName { get; set; }

        public string CaptionName { get; set; }

        public string PathDescription { get; set; }

        public string LinkDescription { get; set; }

        public string CaptionDescription { get; set; }

        public bool UseMobile { get; set; }

        public override OperationResult Validate(string propertyName, string propertyBinding)
        {
            var errors = new List<ErrorMessage>();

            int i = 0;
            while (i < Items.Count)
            {
                var item = Items[i];
                if (item.IsEmpty)
                {
                    Items.Remove(item);
                }
                else
                {
                    i++;
                }
            }

            if (IsRequired)
            {
                if (ItemName.Length == 0)
                {
                    errors.Add(ErrorMessage.Create($"{propertyBinding}", $"{propertyName} is mandatory"));
                }
            }
            
            if (errors.Count == 0)
            {
                return OperationResult.MakeSuccess();
            }
            else
            {
                return OperationResult.MakeFailure(errors);
            }
        }

        public override object Clone()
        {
            return new GalleryAtom()
            {
                Id = Id,
                IsRequired = IsRequired,
                ItemName = ItemName,
                Description = Description,
                CaptionDescription = CaptionDescription,
                CaptionName = CaptionName,
                LinkDescription = LinkDescription,
                LinkName = LinkName,   
                PathDescription = PathDescription,
                PathName = PathName,
                UseMobile = UseMobile,
            };
        }
    }
}
