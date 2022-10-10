using Resulz;

namespace TrinityText.Business.Schema
{
    public abstract class BaseAtom<T> : IAtom where T : IAtom
    {
        public BaseAtom(AtomType type)
        {
            this.Type = type;
        }
        public bool IsRequired { get; set; }

        public string Description { get; set; }

        public string Id { get; set; }
        public AtomType Type { get; private set; }
        public abstract OperationResult Validate(string id, string propertyBindingName);

        public abstract object Clone();
    }

    public enum AtomType
    {
        Text,
        Number,
        Image,
        Gallery,
        Checkbox,
        //Blog,
    }
}
