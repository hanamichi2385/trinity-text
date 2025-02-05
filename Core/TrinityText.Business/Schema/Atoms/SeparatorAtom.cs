using Resulz;


namespace TrinityText.Business.Schema
{
    public class SeparatorAtom : BaseAtom<SeparatorAtom>
    {
        public SeparatorAtom() : base(AtomType.Separator)
        {

        }

        public override object Clone()
        {
            return new SeparatorAtom()
            {
                Id = Id,
            };
        }

        public override OperationResult Validate(string id, string propertyBindingName)
        {
            return OperationResult.MakeSuccess();
        }
    }
}
