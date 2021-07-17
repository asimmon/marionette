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
            if (context.SyntaxContextReceiver is not LibrarySyntaxReceiver receiver)
            {
                return;
            }

            foreach (var targetedClass in receiver.TargetedClasses)
            {
                AddSource(context, LibraryCodeGenerator.Generate(targetedClass));
            }
        }

        private static void AddSource(GeneratorExecutionContext context, CodeGeneratorResult result)
        {
            if (result.Warnings.Count > 0)
            {
                throw new Exception(string.Join(", ", result.Warnings));
            }

            context.AddSource(result.Filename, SourceText.From(result.Code, Encoding.UTF8));
        }
    }
}
