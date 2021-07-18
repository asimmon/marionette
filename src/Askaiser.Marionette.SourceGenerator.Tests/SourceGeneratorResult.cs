using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public sealed class SourceGeneratorResult
    {
        public SourceGeneratorResult(IEnumerable<Diagnostic> diagnostics1, IEnumerable<Diagnostic> diagnostics2, IEnumerable<GeneratedSourceFile> sourceFiles)
        {
            this.Diagnostics1 = diagnostics1.ToList();
            this.Diagnostics2 = diagnostics2.ToList();
            this.SourceFiles = sourceFiles.ToList();
        }

        public IReadOnlyList<Diagnostic> Diagnostics1 { get; }

        public IReadOnlyList<Diagnostic> Diagnostics2 { get; }

        public IReadOnlyList<GeneratedSourceFile> SourceFiles { get; }
    }
}
