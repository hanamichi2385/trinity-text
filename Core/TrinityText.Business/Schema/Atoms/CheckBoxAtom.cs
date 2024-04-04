using Resulz;
using System.Collections.Generic;

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
                if (string.IsNullOrWhiteSpace(Value))
                {
                    errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} is mandatory"));
                }
            }

            //if (bool.TryParse(Value, out bool boolean) == false)
            //{
            //    //errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} not valid format (boolean)"));
            //    boolean = false;
            //}

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
            return new CheckBoxAtom()
            {
                Id = Id,
                IsRequired = IsRequired,
                Description = Description
            };
        }
    }
}
