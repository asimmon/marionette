using System;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

[assembly: InternalsVisibleTo("Askaiser.Marionette.SourceGenerator.Tests")]

namespace Askaiser.Marionette.SourceGenerator
{
    [Generator]
    public class LibrarySourceGenerator : ISourceGenerator
    {
        private readonly IFileSystem _fileSystem;
        private readonly IDateTimeProvider _dateTimeProvider;

        internal LibrarySourceGenerator(IFileSystem fileSystem, IDateTimeProvider dateTimeProvider)
        {
            this._fileSystem = fileSystem;
            this._dateTimeProvider = dateTimeProvider;
        }

        public LibrarySourceGenerator()
            : this(new FileSystem(), new UtcDateTimeProvider())
        {
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new LibrarySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not LibrarySyntaxReceiver receiver)
            {
                return;
            }

            foreach (var targetedClass in receiver.TargetedClasses)
            {
                this.AddSource(context, new LibraryCodeGenerator(this._fileSystem, this._dateTimeProvider, targetedClass).Generate());
            }
        }

        protected virtual void AddSource(GeneratorExecutionContext context, CodeGeneratorResult result)
        {
            if (result.Warnings.Count > 0)
            {
                throw new Exception(string.Join(", ", result.Warnings));
            }

            context.AddSource(result.Filename, SourceText.From(result.Code, Encoding.UTF8));
        }
    }
}
