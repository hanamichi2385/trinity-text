﻿using Resulz;
using System.Collections.Generic;

namespace TrinityText.Business.Schema
{
    public class NumberAtom : BaseAtom<NumberAtom>
    {
        public NumberAtom() : base(AtomType.Number)
        {

        }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
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

            bool validFormat = int.TryParse(Value, out int integer);

            if (validFormat)
            {
                if (MinValue.HasValue)
                {
                    if (integer < MinValue.Value)
                    {
                        errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} min value must be {MinValue.Value}"));
                    }
                }

                if (MaxValue.HasValue)
                {
                    if (integer > MaxValue.Value)
                    {
                        errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} max value must be {MaxValue.Value}"));
                    }
                }
            }
            else
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

        public override object Clone()
        {
            return new NumberAtom()
            {
                Id = Id,
                IsRequired = IsRequired,
                MaxValue = MaxValue,
                MinValue = MinValue,
                Description = Description
            };
        }
    }
}
