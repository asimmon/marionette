using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Askaiser.Marionette.SourceGenerator
{
    public class LibrarySyntaxReceiver : ISyntaxContextReceiver
    {
        private readonly List<TargetedClassInfo> _targetedClasses;
        private readonly List<Diagnostic> _diagnostics;

        public LibrarySyntaxReceiver()
        {
            this._targetedClasses = new List<TargetedClassInfo>();
            this._diagnostics = new List<Diagnostic>();
        }

        public IReadOnlyCollection<TargetedClassInfo> TargetedClasses
        {
            get => this._targetedClasses;
        }

        public IReadOnlyCollection<Diagnostic> Diagnostics
        {
            get => this._diagnostics;
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classSyntax)
            {
                return;
            }

            if (ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classSyntax) is not INamedTypeSymbol classModel)
            {
                return;
            }

            foreach (var attribute in classModel.GetAttributes())
            {
                if (attribute.AttributeClass is null)
                {
                    continue;
                }

                if (!Constants.ExpectedAttributeFullName.Equals(attribute.AttributeClass.GetFullName(), StringComparison.Ordinal))
                {
                    continue;
                }

                if (!classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    this._diagnostics.Add(Diagnostic.Create(DiagnosticsDescriptors.MissingPartialModifier, context.Node.GetLocation(), classModel.GetFullName()));
                    return;
                }

                if (attribute.ConstructorArguments.Length == 0)
                {
                    continue;
                }

                var rawImageDirPath = attribute.ConstructorArguments[0].Value as string;
                if (rawImageDirPath != null && TryValidatePath(rawImageDirPath, out var validImageDirPath))
                {
                    if (!Path.IsPathRooted(validImageDirPath) && classSyntax.SyntaxTree.FilePath is { Length: > 0 } codeFilePath && Path.GetDirectoryName(codeFilePath) is { Length: > 0 } codeDirPath)
                    {
                        validImageDirPath = Path.Combine(codeDirPath, validImageDirPath);
                    }

                    this._targetedClasses.Add(new TargetedClassInfo
                    {
                        ClassName = classModel.Name,
                        NamespaceName = classModel.GetNamespace(),
                        ImageDirectoryPath = validImageDirPath,
                        SyntaxNode = context.Node,
                    });

                    return;
                }

                this._diagnostics.Add(Diagnostic.Create(DiagnosticsDescriptors.InvalidDirectoryPath, context.Node.GetLocation(), rawImageDirPath));
            }
        }

        private static bool TryValidatePath(string rawPath, out string validPath)
        {
            validPath = null;

            if (rawPath.Trim() is not { Length: > 0 } trimmedPath)
            {
                return false;
            }

            try
            {
                _ = Path.GetFullPath(trimmedPath);
            }
            catch
            {
                return false;
            }

            validPath = trimmedPath;
            return true;
        }
    }
}
