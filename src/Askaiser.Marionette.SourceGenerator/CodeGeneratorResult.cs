using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Marionette.SourceGenerator
{
    public sealed class CodeGeneratorResult
    {
        public CodeGeneratorResult(string filename, string code, IEnumerable<string> warnings)
        {
            this.Filename = filename;
            this.Code = code;
            this.Warnings = warnings.ToList();
        }

        public string Filename { get; }

        public string Code { get; }

        // TODO Add warnings to source generator diagnostic entries
        public IReadOnlyCollection<string> Warnings { get; }
    }
}
