using Resulz;
using System.Collections.Generic;

namespace TrinityText.Business.Schema
{
    public class ImageAtom : BaseAtom<ImageAtom>
    {
        public ImageAtom() : base(AtomType.Image)
        {

        }
        public string Link { get; set; }

        public string Caption { get; set; }

        public string Value { get; set; }

        public string Mobile { get; set; }

        public string Target { get; set; }

        public bool UseMobile { get; set; }

        public override OperationResult Validate(string propertyName, string propertyBinding)
        {
            var errors = new List<ErrorMessage>();

            if (IsRequired)
            {
                if (string.IsNullOrWhiteSpace(Value))
                {
                    errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} is mandatory"));
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
            return new ImageAtom()
            {
                Id = Id,
                IsRequired = IsRequired,
                Description = Description,
                UseMobile = UseMobile,
            };
        }
    }
}
