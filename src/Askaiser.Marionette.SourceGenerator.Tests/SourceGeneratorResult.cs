using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator.Tests;

public sealed class SourceGeneratorResult
{
    public SourceGeneratorResult(IEnumerable<Diagnostic> diagnostics, IEnumerable<GeneratedSourceFile> sourceFiles)
    {
        this.Diagnostics = diagnostics.ToList();
        this.SourceFiles = sourceFiles.ToList();
    }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public IReadOnlyList<GeneratedSourceFile> SourceFiles { get; }
}
