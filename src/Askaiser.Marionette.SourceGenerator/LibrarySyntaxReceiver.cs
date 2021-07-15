using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Askaiser.Marionette.SourceGenerator
{
    public class LibrarySyntaxReceiver : ISyntaxContextReceiver
    {
        private const string ExpectedAttributeFullName = "Askaiser.Marionette.ImageLibraryAttribute";

        public LibrarySyntaxReceiver()
        {
            this.Success = false;
            this.DecoratedClassName = null;
            this.DecoratedNamespaceName = null;
            this.ImageLibraryDirectoryPath = null;
        }

        public bool Success { get; private set; }
        public string DecoratedNamespaceName { get; private set; }
        public string DecoratedClassName { get; private set; }
        public string ImageLibraryDirectoryPath { get; private set; }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (this.Success)
                return;

            if (context.Node is not ClassDeclarationSyntax classSyntax)
                return;

            if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not { } classModel)
                return;

            foreach (var attr in classModel.GetAttributes())
            {
                if (attr.AttributeClass is { } attrClass)
                {
                    if (string.Equals(ExpectedAttributeFullName, attrClass.GetFullName(), StringComparison.Ordinal))
                    {
                        if (attr.ConstructorArguments is { Length: > 0 } ctorArgs && ctorArgs[0].Value is string rawPath && TrySanitizePath(rawPath, out var sanitizedPath))
                        {
                            this.Success = true;
                            this.DecoratedClassName = classModel.Name;
                            this.DecoratedNamespaceName = classModel.GetNamespace();
                            this.ImageLibraryDirectoryPath = sanitizedPath;

                            if (!Path.IsPathRooted(sanitizedPath))
                            {
                                if (classSyntax.SyntaxTree.FilePath is { Length: > 0 } codeFilePath && Path.GetDirectoryName(codeFilePath) is { Length: > 0 } codeDirPath)
                                {
                                    this.ImageLibraryDirectoryPath = Path.Combine(codeDirPath, sanitizedPath);
                                }
                            }

                            return;
                        }
                    }
                }
            }
        }

        private static bool TrySanitizePath(string rawPath, out string validPath)
        {
            validPath = default;

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
