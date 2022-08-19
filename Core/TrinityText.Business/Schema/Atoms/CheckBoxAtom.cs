using Resulz;
using System.Collections.Generic;
using System.Linq;

namespace TrinityText.Business.Schema
{
    public class CheckBoxAtom : BaseAtom<CheckBoxAtom>
    {
        public CheckBoxAtom() : base(AtomType.Checkbox)
        {

        }

        public string Value { get; set; }

        public override OperationResult Validate(string propertyName, string propertyBinding)
        {
            var errors = new List<ErrorMessage>();

            if (IsRequired)
            {
                if (string.IsNullOrEmpty(Value))
                {
                    errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} is mandatory"));
                }
            }

            bool boolean = false;

            if (bool.TryParse(Value, out boolean) == false)
            {
                errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} not valid format (boolean)"));
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
            return new CheckBoxAtom()
            {
                Id = Id,
                IsRequired = IsRequired,
            };
        }
    }
}
