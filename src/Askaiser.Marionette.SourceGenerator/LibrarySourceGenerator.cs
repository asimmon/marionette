using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Askaiser.Marionette.SourceGenerator
{
    [Generator]
    public class LibrarySourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new LibrarySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not LibrarySyntaxReceiver { Success: true } receiver)
                return;

            var result = LibraryCodeGenerator.Generate(new LibraryCodeGeneratorOptions
            {
                ImageDirectoryPath = receiver.ImageLibraryDirectoryPath,
                ClassName = receiver.DecoratedClassName,
                NamespaceName = receiver.DecoratedNamespaceName,
            });

            if (result.Warnings.Count > 0)
            {
                throw new Exception(string.Join(", ", result.Warnings));
            }

            context.AddSource(nameof(LibrarySourceGenerator), SourceText.From(result.Code, Encoding.UTF8));
        }
    }
}
