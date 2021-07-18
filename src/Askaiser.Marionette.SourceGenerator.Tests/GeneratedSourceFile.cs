namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public sealed class GeneratedSourceFile
    {
        public GeneratedSourceFile(string filename, string code)
        {
            this.Filename = filename;
            this.Code = code;
        }

        public string Filename { get; }

        public string Code { get; }
    }
}
