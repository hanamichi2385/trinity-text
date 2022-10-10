using Resulz;

namespace TrinityText.Business.Schema
{
    public interface IAtom
    {
        bool IsRequired { get; set; }
        string Id { get; set; }
        AtomType Type { get; }
        string Description { get; set; }
        OperationResult Validate(string id, string propertyBindingName);
        object Clone();
    }


}