using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator;

public sealed class TargetedClassInfo
{
    public TargetedClassInfo(string className, string namespaceName, string modifierNames, string imageDirectoryPath, bool isSingleton, SyntaxNode syntaxNode)
    {
        this.MaxImageSize = Constants.DefaultMaxImageSize;
        this.ClassName = className;
        this.NamespaceName = namespaceName;
        this.ModifierNames = modifierNames;
        this.ImageDirectoryPath = imageDirectoryPath;
        this.IsSingleton = isSingleton;
        this.SyntaxNode = syntaxNode;
    }

    public long MaxImageSize { get; }

    public string NamespaceName { get; }

    public string ClassName { get; }

    public string ModifierNames { get; }

    public string ImageDirectoryPath { get; }

    public bool IsSingleton { get; }

    public SyntaxNode SyntaxNode { get; }
}
