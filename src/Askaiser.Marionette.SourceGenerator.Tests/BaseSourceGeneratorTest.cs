using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public abstract class BaseSourceGeneratorTest
    {
        protected BaseSourceGeneratorTest()
        {
            this.FileSystem = new FakeFileSystem();
            this.SourceGenerator = new TestableLibrarySourceGenerator(this.FileSystem);
        }

        protected FakeFileSystem FileSystem { get; }

        protected TestableLibrarySourceGenerator SourceGenerator { get; }

        protected SourceGeneratorResult Compile(string code)
        {
            var initialCompilation = CreateCompilation(code);
            var driver = CreateDriver(this.SourceGenerator);
            var updatedCompilation = RunGenerators(driver, initialCompilation, out var diagnostics1);
            var diagnostics2 = updatedCompilation.GetDiagnostics();

            return new SourceGeneratorResult(diagnostics1, diagnostics2, this.SourceGenerator.GeneratedSources);
        }

        private static Compilation CreateCompilation(string source) => CSharpCompilation.Create(
            assemblyName: "compilation",
            syntaxTrees: ImmutableArray.Create(new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp9)) }),
            references: GetRequiredAssemblyReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        private static GeneratorDriver CreateDriver(ISourceGenerator generator) => CSharpGeneratorDriver.Create(generator);

        private static IEnumerable<MetadataReference> GetRequiredAssemblyReferences() => AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Concat(new[] { MetadataReference.CreateFromFile(typeof(ImageLibraryAttribute).GetTypeInfo().Assembly.Location) });

        private static Compilation RunGenerators(GeneratorDriver driver, Compilation compilation, out ImmutableArray<Diagnostic> diagnostics)
        {
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out diagnostics);
            return updatedCompilation;
        }
    }
}
