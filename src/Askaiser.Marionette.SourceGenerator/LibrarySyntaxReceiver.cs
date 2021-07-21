using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var attribute = FindImageLibraryAttribute(classModel);
            if (attribute == null)
            {
                return;
            }

            if (!classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                this._diagnostics.Add(Diagnostic.Create(DiagnosticsDescriptors.MissingPartialModifier, context.Node.GetLocation(), classModel.GetFullName()));
                return;
            }

            if (classSyntax.Parent is ClassDeclarationSyntax)
            {
                this._diagnostics.Add(Diagnostic.Create(DiagnosticsDescriptors.NestedClassNotAllowed, context.Node.GetLocation()));
                return;
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
            }
            else
            {
                this._diagnostics.Add(Diagnostic.Create(DiagnosticsDescriptors.InvalidDirectoryPath, context.Node.GetLocation(), rawImageDirPath));
            }
        }

        private static AttributeData FindImageLibraryAttribute(ISymbol symbol)
        {
            return symbol.GetAttributes().FirstOrDefault(IsImageLibraryAttribute);
        }

        private static bool IsImageLibraryAttribute(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length == 0)
            {
                return false;
            }

            if (attribute.AttributeClass is null)
            {
                return false;
            }

            if (!Constants.ExpectedAttributeName.Equals(attribute.AttributeClass.Name, StringComparison.Ordinal))
            {
                return false;
            }

            return Constants.ExpectedAttributeNamespaceName.Equals(attribute.AttributeClass.GetNamespace(), StringComparison.Ordinal);
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
