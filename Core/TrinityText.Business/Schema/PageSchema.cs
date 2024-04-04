using Resulz;
using System.Collections.Generic;
using System.Linq;

namespace TrinityText.Business.Schema
{
    public class PageSchema
    {
        public PageSchema()
        {
            this.Body = Enumerable.Empty<IAtom>().ToList();
        }

        public int UniqueIdentifier { get; set; }

        public string RootName { get; set; }

        public string ChildName { get; set; }

        public IList<IAtom> Body { get; private set; }

        public OperationResult Validate()
        {
            var isValid = OperationResult.MakeSuccess();

            for (int i = 0; i < Body.Count; i++)
            {
                var v = Body.ElementAt(i);

                var validProperty = v.Validate(v.Id, $"PageSchema.Body[{i}]");

                isValid.AppendErrors(validProperty.Errors);
            }

            return isValid;
        }
    }
}
