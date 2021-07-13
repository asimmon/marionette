using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Puppets
{
    public sealed class CodeGenerationResult
    {
        public CodeGenerationResult(string code, IEnumerable<string> warnings)
        {
            this.Code = code;
            this.Warnings = warnings.ToList();
        }

        public string Code { get; }

        public IReadOnlyCollection<string> Warnings { get; }
    }
}
