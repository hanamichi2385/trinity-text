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

        public const string Format = "dd/MM/yyyy HH:mm:ss";

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
                else
                {
                    ValidateDateTime(propertyName, propertyBinding, errors);
                }
            }
            else
            {
                ValidateDateTime(propertyName, propertyBinding, errors);
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

        private void ValidateDateTime(string propertyName, string propertyBinding, List<ErrorMessage> errors)
        {
            if (DateTime.TryParseExact(Value, Format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _) == false)
            {
                errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} not valid format ({Format})"));
            }
        }
    }
}
