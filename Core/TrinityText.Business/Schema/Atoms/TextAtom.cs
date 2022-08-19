using Resulz;
using System.Collections.Generic;
using System.Linq;

namespace TrinityText.Business.Schema
{
    public class TextAtom : BaseAtom<TextAtom>
    {
        public TextAtom() : base(AtomType.Text)
        {

        }

        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public bool IsHtml { get; set; }
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

            var length = string.IsNullOrEmpty(Value) ? 0 : Value.Length;

            if (MinValue.HasValue)
            {
                if (length < MinValue.Value)
                {
                    errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} min length must be {MinValue.Value}"));
                }
            }

            if (MaxValue.HasValue)
            {
                if (length > MaxValue.Value)
                {
                    errors.Add(ErrorMessage.Create($"{propertyBinding}.Value", $"{propertyName} max length must be {MaxValue.Value}"));
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
            return new TextAtom()
            {
                Id = Id,
                IsHtml = IsHtml,
                IsRequired = IsRequired,
                MinValue = MinValue,
                MaxValue = MaxValue,
            };
        }
    }
}
