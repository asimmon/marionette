using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public class TestableLibrarySourceGenerator : LibrarySourceGenerator
    {
        private readonly List<GeneratedSourceFile> _generatedSources;

        internal TestableLibrarySourceGenerator(IFileSystem fileSystem)
            : base(fileSystem)
        {
            this._generatedSources = new List<GeneratedSourceFile>();
        }

        public IReadOnlyList<GeneratedSourceFile> GeneratedSources
        {
            get => this._generatedSources;
        }

        protected override void AddSource(GeneratorExecutionContext context, CodeGeneratorResult result)
        {
            base.AddSource(context, result);
            this._generatedSources.Add(new GeneratedSourceFile(result.Filename, result.Code));
        }
    }
}
