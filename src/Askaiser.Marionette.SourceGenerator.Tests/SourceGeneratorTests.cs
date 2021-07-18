using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public class SourceGeneratorTests
    {
        [Fact]
        public void Nothing()
        {
            var userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""foo"")]
    public partial class MyLibrary
    {
    }
}";
            var comp = CreateCompilation(userSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new LibrarySourceGenerator());

            Assert.Empty(generatorDiags);

            var diags = newComp.GetDiagnostics();
            Assert.Empty(diags);
        }

        private static Compilation CreateCompilation(string source) => CSharpCompilation.Create(
            assemblyName: "compilation",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp9)) },
            references: GetRequiredAssemblyReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        private static GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators) => CSharpGeneratorDriver.Create(
            generators: ImmutableArray.Create(generators),
            additionalTexts: ImmutableArray<AdditionalText>.Empty,
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
            optionsProvider: null);

        private static IEnumerable<MetadataReference> GetRequiredAssemblyReferences() => AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(ImageLibraryAttribute).GetTypeInfo().Assembly.Location),
            });

        private static Compilation RunGenerators(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(compilation, generators).RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out diagnostics);
            return updatedCompilation;
        }
    }
}
