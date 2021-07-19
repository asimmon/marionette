namespace Askaiser.Marionette.SourceGenerator
{
    public sealed class TargetedClassInfo
    {
        internal const long DefaultMaxImageSize = 2 * 1024 * 1024;

        public TargetedClassInfo()
        {
            this.MaxImageSize = DefaultMaxImageSize;
        }

        public long MaxImageSize { get; set; }

        public string NamespaceName { get; set; }

        public string ClassName { get; set; }

        public string ImageDirectoryPath { get; set; }
    }
}
