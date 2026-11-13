using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Askaiser.Marionette.SourceGenerator.Tests;

/// <summary>
/// Runs the generator on source code, the way the compiler does, and gives access to what it produced.
/// </summary>
internal sealed class GeneratorHarness
{
    /// <summary>The directory of the source file that tests compile. Relative image directories are resolved from it.</summary>
    public const string SourceDirectory = "/project/src";

    private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

    private readonly CSharpParseOptions _parseOptions;
    private GeneratorDriver _driver;

    public GeneratorHarness(FakeFileSystem? fileSystem = null, LanguageVersion languageVersion = LanguageVersion.Latest)
    {
        this.FileSystem = fileSystem ?? new FakeFileSystem();
        this._parseOptions = new CSharpParseOptions(languageVersion);

        this._driver = CSharpGeneratorDriver.Create(
            [new ImageLibraryGenerator(this.FileSystem, TimeSpan.Zero).AsSourceGenerator()],
            parseOptions: this._parseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    }

    public FakeFileSystem FileSystem { get; }

    /// <summary>
    /// Compiles source code with the generator. Calling this method several times behaves like successive edits in an IDE:
    /// the generator keeps what it computed during the previous calls.
    /// </summary>
    public GeneratorOutcome Run(params string[] sources)
    {
        var trees = sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, this._parseOptions, path: $"{SourceDirectory}/File{index}.cs"));
        var compilation = CSharpCompilation.Create("GeneratorTests", trees, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        this._driver = this._driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var generatorDiagnostics);
        var result = this._driver.GetRunResult().Results.Single();

        return new GeneratorOutcome(
            updatedCompilation,
            generatorDiagnostics,
            result.GeneratedSources.ToDictionary(source => source.HintName, source => source.SourceText.ToString()),
            result.TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs).Select(output => output.Reason).ToList());
    }

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);

        return
        [
            .. trustedAssemblies
                .Where(path => Path.GetFileName(path) is "System.Runtime.dll" or "System.Private.CoreLib.dll" or "netstandard.dll" or "System.Memory.dll" or "System.Collections.dll" or "System.Linq.dll")
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)),
            MetadataReference.CreateFromFile(typeof(ImageElement).Assembly.Location),
        ];
    }
}
