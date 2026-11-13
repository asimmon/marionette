using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator.Tests;

internal sealed record GeneratorOutcome(Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics, Dictionary<string, string> Sources, List<IncrementalStepRunReason> OutputReasons)
{
    /// <summary>Gets the errors and warnings of the compilation, generated code included.</summary>
    public IEnumerable<Diagnostic> CompilationProblems => this.Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);

    public string SingleSource => this.Sources.Values.Single();

    /// <summary>
    /// Loads the compiled assembly and evaluates a chain of members, starting from a static member: <c>MyCode.MyLibrary.Instance.Pages.Title</c>.
    /// </summary>
    public object? Evaluate(string typeName, params string[] members)
    {
        using var stream = new MemoryStream();
        var emitResult = this.Compilation.Emit(stream);
        if (!emitResult.Success)
        {
            throw new InvalidOperationException("The code does not compile: " + string.Join(Environment.NewLine, emitResult.Diagnostics));
        }

        stream.Position = 0;
        var context = new AssemblyLoadContext(name: null, isCollectible: true);
        var type = context.LoadFromStream(stream).GetType(typeName, throwOnError: true)!;

        object? current = null;
        var currentType = type;
        foreach (var member in members)
        {
            var property = currentType.GetProperty(member, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"The type '{currentType}' has no public property named '{member}'.");

            current = property.GetValue(current);
            currentType = current?.GetType() ?? property.PropertyType;
        }

        return current;
    }
}
