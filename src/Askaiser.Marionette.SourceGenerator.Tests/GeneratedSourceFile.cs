namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public sealed class GeneratedSourceFile
    {
        public GeneratedSourceFile(string filename, string code)
        {
            Filename = filename;
            Code = code;
        }

        public string Filename { get; }

        public string Code { get; }
    }
}
