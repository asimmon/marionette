using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator
{
    public sealed class CodeGeneratorResult
    {
        public CodeGeneratorResult(string filename, string code, IEnumerable<Diagnostic> diagnostics)
        {
            this.Filename = filename;
            this.Code = code;
            this.Diagnostics = diagnostics.ToList();
        }

        public string Filename { get; }

        public string Code { get; }

        public IReadOnlyCollection<Diagnostic> Diagnostics { get; }
    }
}
