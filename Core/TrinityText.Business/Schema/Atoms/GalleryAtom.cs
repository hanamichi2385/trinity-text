using Resulz;
using System.Collections.Generic;
using System.Linq;

namespace TrinityText.Business.Schema
{
    public class GalleryAtom : BaseAtom<GalleryAtom>
    {
        public GalleryAtom() : base(AtomType.Gallery)
        {

        }

        public IList<Particol> Items { get; private set; }
        public string ItemName { get; set; }

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
                if (ItemName.Any() == false)
                {
                    errors.Add(ErrorMessage.Create($"{propertyBinding}", $"{propertyName} is mandatory"));
                }
            }
            
            if (errors.Any() == false)
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
            };
        }
    }
}
