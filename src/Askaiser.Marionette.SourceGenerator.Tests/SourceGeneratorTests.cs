using Xunit;

namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public class SourceGeneratorTests : BaseSourceGeneratorTest
    {
        [Fact]
        public void NoSourceCode_GeneratesNothing()
        {
            var result = this.Compile(string.Empty);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);
            Assert.Empty(result.SourceFiles);
        }

        [Fact]
        public void PartialClassWithoutAttribute_GeneratesNothing()
        {
            const string userSource = @"
namespace MyCode
{
    public partial class MyLibrary { }
}";

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);
            Assert.Empty(result.SourceFiles);
        }

        [Fact]
        public void NotPartialClassWithAttribute_GeneratesNothing()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""ignored"")]
    public class MyLibrary { }
}";

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);
            Assert.Empty(result.SourceFiles);
        }

        [Fact]
        public void WhenNullImageLibraryPath_GeneratesNothing()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(null)]
    public partial class MyLibrary { }
}";

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);
            Assert.Empty(result.SourceFiles);
        }

        [Fact]
        public void WhenEmptyImageLibraryPath_GeneratesNothing()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute("""")]
    public partial class MyLibrary { }
}";

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);
            Assert.Empty(result.SourceFiles);
        }

        [Fact]
        public void WhenNoImagesInCurrentDirectory_GeneratesEmptyLibrary()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
@"// Code generated at 2021-01-01T00:00:00.0000000
// From the directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        protected readonly ElementCollection _elements;

        public MyLibrary()
        {
            this._elements = new ElementCollection();
            this.CreateElements();
        }

        private void CreateElements()
        {
        }
    }
}
";
            Assert.Equal(expectedSource, sourceFile.Code);
        }
    }
}
