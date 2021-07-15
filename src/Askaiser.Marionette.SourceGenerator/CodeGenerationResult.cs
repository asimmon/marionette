using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Marionette.SourceGenerator
{
    public sealed class CodeGenerationResult
    {
        public CodeGenerationResult(string code, IEnumerable<string> warnings)
        {
            this.Code = code;
            this.Warnings = warnings.ToList();
        }

        public string Code { get; }

        // TODO Add warnings to source generator diagnostic entries
        public IReadOnlyCollection<string> Warnings { get; }
    }
}
