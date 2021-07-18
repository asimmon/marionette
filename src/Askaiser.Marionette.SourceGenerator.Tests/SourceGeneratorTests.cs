using System;
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
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

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

        [Fact]
        public void WhenEmptyImageInCurrentDirectory_GeneratesEmptyLibrary()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./logo.png", Array.Empty<byte>());

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

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

        [Fact]
        public void WhenNotAnImageInCurrentDirectory_GeneratesEmptyLibrary()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./readme.md", new byte[] { 0 });

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

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

        [Fact]
        public void WhenSingleImageInCurrentDirectory_GeneratesLibrary()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./logo.png", new byte[] { 1, 2, 3 });

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

        public MyLibrary()
        {
            this._elements = new ElementCollection();
            this.CreateElements();
        }

        public IElement Logo => this._elements[""Root.Logo.0""];

        private void CreateElements()
        {
            this._elements.Add(new ImageElement(""Root.Logo.0"", ""AQID"", 0.95m, false));
        }
    }
}
";
            Assert.Equal(expectedSource, sourceFile.Code);
        }

        [Fact]
        public void WhenSingleImageWithoutNamespace_GeneratesLibrary()
        {
            const string userSource = @"
[Askaiser.Marionette.ImageLibraryAttribute(""."")]
public partial class MyLibrary { }";

            this.FileSystem.SetFileBytes("./logo.png", new byte[] { 1, 2, 3 });

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

public partial class MyLibrary
{
    private readonly ElementCollection _elements;

    public MyLibrary()
    {
        this._elements = new ElementCollection();
        this.CreateElements();
    }

    public IElement Logo => this._elements[""Root.Logo.0""];

    private void CreateElements()
    {
        this._elements.Add(new ImageElement(""Root.Logo.0"", ""AQID"", 0.95m, false));
    }
}
";
            Assert.Equal(expectedSource, sourceFile.Code);
        }

        [Fact]
        public void WhenMultipleImagesInCurrentDirectory_GeneratesLibrary()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./logo.png", new byte[] { 1, 2, 3 });
            this.FileSystem.SetFileBytes("./sidebar.png", new byte[] { 1, 2, 3 });

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

        public MyLibrary()
        {
            this._elements = new ElementCollection();
            this.CreateElements();
        }

        public IElement Logo => this._elements[""Root.Logo.0""];

        public IElement Sidebar => this._elements[""Root.Sidebar.0""];

        private void CreateElements()
        {
            this._elements.Add(new ImageElement(""Root.Logo.0"", ""AQID"", 0.95m, false));
            this._elements.Add(new ImageElement(""Root.Sidebar.0"", ""AQID"", 0.95m, false));
        }
    }
}
";
            Assert.Equal(expectedSource, sourceFile.Code);
        }

        [Fact]
        public void WhenMultipleImagesInDifferentDirectories_GeneratesLibrary()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./foo/logo-large.png", new byte[] { 1, 2, 3 });
            this.FileSystem.SetFileBytes("./bar/sidebar.png", new byte[] { 1, 2, 3 });

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

        public MyLibrary()
        {
            this._elements = new ElementCollection();
            this.Bar = new RootBarLibrary(this._elements);
            this.Foo = new RootFooLibrary(this._elements);

            this.CreateElements();
        }

        public RootBarLibrary Bar { get; }

        public RootFooLibrary Foo { get; }

        private void CreateElements()
        {
            this._elements.Add(new ImageElement(""Root.Bar.Sidebar.0"", ""AQID"", 0.95m, false));
            this._elements.Add(new ImageElement(""Root.Foo.LogoLarge.0"", ""AQID"", 0.95m, false));
        }
    }

    public partial class RootBarLibrary
    {
        private readonly ElementCollection _elements;

        public RootBarLibrary(ElementCollection elements)
        {
            this._elements = elements;
        }

        public IElement Sidebar => this._elements[""Root.Bar.Sidebar.0""];
    }

    public partial class RootFooLibrary
    {
        private readonly ElementCollection _elements;

        public RootFooLibrary(ElementCollection elements)
        {
            this._elements = elements;
        }

        public IElement LogoLarge => this._elements[""Root.Foo.LogoLarge.0""];
    }
}
";
            Assert.Equal(expectedSource, sourceFile.Code);
        }

        [Fact]
        public void WhenMultipleImagesWithSuffixes_GeneratesLibrary()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./logo_0.85_0.png", new byte[] { 1, 2, 3 });
            this.FileSystem.SetFileBytes("./logo_gs_0.78_1.png", new byte[] { 1, 2, 3 });

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

        public MyLibrary()
        {
            this._elements = new ElementCollection();
            this.CreateElements();
        }

        public IElement[] Logo => new[]
        {
            this._elements[""Root.Logo.0""],
            this._elements[""Root.Logo.1""]
        };

        private void CreateElements()
        {
            this._elements.Add(new ImageElement(""Root.Logo.0"", ""AQID"", 0.85m, false));
            this._elements.Add(new ImageElement(""Root.Logo.1"", ""AQID"", 0.78m, true));
        }
    }
}
";
            Assert.Equal(expectedSource, sourceFile.Code);
        }

        [Fact]
        public void WhenSingleImageAndMultipleEmptyDirectories_GeneratesLibraryWithSingleImage()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./foo/logo.png", new byte[] { 1, 2, 3 });
            this.FileSystem.AddEntry("./foo/bar");
            this.FileSystem.AddEntry("./foo/qux/baz");
            this.FileSystem.AddEntry("./empty");

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

        public MyLibrary()
        {
            this._elements = new ElementCollection();
            this.Foo = new RootFooLibrary(this._elements);

            this.CreateElements();
        }

        public RootFooLibrary Foo { get; }

        private void CreateElements()
        {
            this._elements.Add(new ImageElement(""Root.Foo.Logo.0"", ""AQID"", 0.95m, false));
        }
    }

    public partial class RootFooLibrary
    {
        private readonly ElementCollection _elements;

        public RootFooLibrary(ElementCollection elements)
        {
            this._elements = elements;
        }

        public IElement Logo => this._elements[""Root.Foo.Logo.0""];
    }
}
";
            Assert.Equal(expectedSource, sourceFile.Code);
        }

        [Fact]
        public void WhenMultipleImagesWithDoubleCarets_GeneratesLibrary()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./foo--bar--logo.png", new byte[] { 1, 2, 3 });
            this.FileSystem.SetFileBytes("./qux--baz--title.png", new byte[] { 1, 2, 3 });

            var result = this.Compile(userSource);

            Assert.Empty(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);

            var sourceFile = Assert.Single(result.SourceFiles);
            Assert.NotNull(sourceFile);
            Assert.Equal("MyCode.MyLibrary.images.cs", sourceFile.Filename);

            const string expectedSource =
                @"// Code generated at 2021-01-01T00:00:00.0000000
// From directory: .
using Askaiser.Marionette;

namespace MyCode
{
    public partial class MyLibrary
    {
        private readonly ElementCollection _elements;

        public MyLibrary()
        {
            this._elements = new ElementCollection();
            this.Foo = new RootFooLibrary(this._elements);
            this.Qux = new RootQuxLibrary(this._elements);

            this.CreateElements();
        }

        public RootFooLibrary Foo { get; }

        public RootQuxLibrary Qux { get; }

        private void CreateElements()
        {
            this._elements.Add(new ImageElement(""Root.Foo.Bar.Logo.0"", ""AQID"", 0.95m, false));
            this._elements.Add(new ImageElement(""Root.Qux.Baz.Title.0"", ""AQID"", 0.95m, false));
        }
    }

    public partial class RootFooLibrary
    {
        private readonly ElementCollection _elements;

        public RootFooLibrary(ElementCollection elements)
        {
            this._elements = elements;
            this.Bar = new RootFooBarLibrary(this._elements);
        }

        public RootFooBarLibrary Bar { get; }
    }

    public partial class RootFooBarLibrary
    {
        private readonly ElementCollection _elements;

        public RootFooBarLibrary(ElementCollection elements)
        {
            this._elements = elements;
        }

        public IElement Logo => this._elements[""Root.Foo.Bar.Logo.0""];
    }

    public partial class RootQuxLibrary
    {
        private readonly ElementCollection _elements;

        public RootQuxLibrary(ElementCollection elements)
        {
            this._elements = elements;
            this.Baz = new RootQuxBazLibrary(this._elements);
        }

        public RootQuxBazLibrary Baz { get; }
    }

    public partial class RootQuxBazLibrary
    {
        private readonly ElementCollection _elements;

        public RootQuxBazLibrary(ElementCollection elements)
        {
            this._elements = elements;
        }

        public IElement Title => this._elements[""Root.Qux.Baz.Title.0""];
    }
}
";
            Assert.Equal(expectedSource, sourceFile.Code);
        }

        [Fact]
        public void WhenDuplicateImageIndexes_AddsDiagnostic()
        {
            const string userSource = @"
namespace MyCode
{
    [Askaiser.Marionette.ImageLibraryAttribute(""."")]
    public partial class MyLibrary { }
}";

            this.FileSystem.SetFileBytes("./foo_0.99_gs_0.png", new byte[] { 1, 2, 3 });
            this.FileSystem.SetFileBytes("./foo_0.80_0.png", new byte[] { 1, 2, 3 });

            var result = this.Compile(userSource);

            Assert.Single(result.Diagnostics1);
            Assert.Empty(result.Diagnostics2);
            Assert.Empty(result.SourceFiles);
        }
    }
}
