namespace Askaiser.Marionette.SourceGenerator
{
    public sealed class TargetedClassInfo
    {
        public TargetedClassInfo()
        {
            this.MaxImageSize = 2 * 1024 * 1024;
        }

        public long MaxImageSize { get; set; }

        public string NamespaceName { get; set; }

        public string ClassName { get; set; }

        public string ImageDirectoryPath { get; set; }
    }
}
