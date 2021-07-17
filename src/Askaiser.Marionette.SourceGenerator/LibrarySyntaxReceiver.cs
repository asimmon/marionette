using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Askaiser.Marionette.SourceGenerator
{
    public class LibrarySyntaxReceiver : ISyntaxContextReceiver
    {
        private const string ExpectedAttributeFullName = "Askaiser.Marionette.ImageLibraryAttribute";

        private readonly List<TargetedClassInfo> _targetedClasses;

        public LibrarySyntaxReceiver()
        {
            this._targetedClasses = new List<TargetedClassInfo>();
        }

        public IReadOnlyCollection<TargetedClassInfo> TargetedClasses
        {
            get => this._targetedClasses;
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classSyntax)
                return;

            if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not { } classModel)
                return;

            foreach (var attribute in classModel.GetAttributes())
            {
                if (attribute.AttributeClass is { } attributeClass &&
                    ExpectedAttributeFullName.Equals(attributeClass.GetFullName(), StringComparison.Ordinal) &&
                    attribute.ConstructorArguments is { Length: > 0 } constructorArguments &&
                    constructorArguments[0].Value is string rawImageDirPath &&
                    TryValidatePath(rawImageDirPath, out var validImageDirPath))
                {
                    if (!Path.IsPathRooted(validImageDirPath) && classSyntax.SyntaxTree.FilePath is { Length: > 0 } codeFilePath && Path.GetDirectoryName(codeFilePath) is { Length: > 0 } codeDirPath)
                    {
                        validImageDirPath = Path.Combine(codeDirPath, validImageDirPath);
                    }

                    this._targetedClasses.Add(new TargetedClassInfo
                    {
                        ClassName = classModel.Name,
                        NamespaceName = classModel.GetNamespace(),
                        ImageDirectoryPath = validImageDirPath
                    });

                    return;
                }
            }
        }

        private static bool TryValidatePath(string rawPath, out string validPath)
        {
            validPath = null;

            if (rawPath.Trim() is not { Length: > 0 } trimmedPath)
                return false;

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
