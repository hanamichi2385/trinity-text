using Resulz;
using System;
using System.Collections.Generic;

namespace TrinityText.Business.Schema
{
    public class DateTimeAtom : BaseAtom<DateTimeAtom>
    {
        public DateTimeAtom() : base(AtomType.DateTime)
        {
        }

        public string Value { get; set; }   

        public override object Clone()
        {
            return new DateTimeAtom()
            {
                Id = Id,
                IsRequired = IsRequired,
                Description = Description
            };
        }

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

            if (DateTime.TryParseExact(Value, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _) == false)
            {
                errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} not valid format (integer)"));
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
    }
}
