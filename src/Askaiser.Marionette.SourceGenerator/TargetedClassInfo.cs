using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator
{
    public sealed class TargetedClassInfo
    {
        public TargetedClassInfo()
        {
            this.MaxImageSize = Constants.DefaultMaxImageSize;
        }

        public long MaxImageSize { get; set; }

        public string NamespaceName { get; set; }

        public string ClassName { get; set; }

        public string ImageDirectoryPath { get; set; }

        public SyntaxNode SyntaxNode { get; set; }
    }
}
