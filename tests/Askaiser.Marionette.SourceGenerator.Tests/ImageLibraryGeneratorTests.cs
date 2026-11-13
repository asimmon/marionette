using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.Enums;

namespace Askaiser.Marionette.SourceGenerator.Tests;

public class ImageLibraryGeneratorTests
{
    private const string LibrarySource = """
        namespace MyCode
        {
            [Askaiser.Marionette.ImageLibrary("images")]
            public partial class MyLibrary
            {
            }
        }
        """;

    private const string ImagesDirectory = GeneratorHarness.SourceDirectory + "/images";

    private static readonly byte[] FirstImage = [1, 2, 3, 4, 5];
    private static readonly byte[] SecondImage = [200, 201, 202];
    private static readonly byte[] ThirdImage = [9];

    [Test]
    public async Task WhenNoAttribute_GeneratesNothing()
    {
        var outcome = new GeneratorHarness().Run("namespace MyCode { public partial class MyLibrary { } }");

        await Assert.That(outcome.Diagnostics).IsEmpty();
        await Assert.That(outcome.Sources).IsEmpty();
    }

    [Test]
    public async Task WhenDirectoryIsEmpty_GeneratesALibraryWithoutImages()
    {
        var harness = new GeneratorHarness(new FakeFileSystem().AddDirectory(ImagesDirectory));

        var outcome = harness.Run(LibrarySource);

        await Assert.That(outcome.Diagnostics).IsEmpty();
        await Assert.That(outcome.CompilationProblems).IsEmpty();
        await Assert.That(outcome.Sources.Keys.Single()).IsEqualTo("MyCode.MyLibrary.images.g.cs");

        var instance = outcome.Evaluate("MyCode.MyLibrary", "Instance");
        await Assert.That(instance).IsNotNull();
        await Assert.That(instance!.GetType().GetProperties().Select(property => property.Name)).IsEquivalentTo(["Instance"]);
    }

    [Test]
    [Arguments(LanguageVersion.Latest)]
    [Arguments(LanguageVersion.CSharp11)]
    [Arguments(LanguageVersion.CSharp10)]
    [Arguments(LanguageVersion.CSharp8)]
    public async Task WhenSingleImage_GeneratesAnElementProperty(LanguageVersion languageVersion)
    {
        var fileSystem = new FakeFileSystem().AddFile(ImagesDirectory + "/ok-button_gs_0.8.png", FirstImage);
        var harness = new GeneratorHarness(fileSystem, languageVersion);

        var outcome = harness.Run(LibrarySource);

        await Assert.That(outcome.Diagnostics).IsEmpty();
        await Assert.That(outcome.CompilationProblems).IsEmpty();

        // UTF-8 literals, which avoid allocating a string per image, only exist since C# 11.
        await Assert.That(outcome.SingleSource.Contains("\"u8", StringComparison.Ordinal)).IsEqualTo(languageVersion is LanguageVersion.Latest or LanguageVersion.CSharp11);

        var element = (ImageElement)outcome.Evaluate("MyCode.MyLibrary", "Instance", "OkButton")!;
        await Assert.That(element.Name).IsEqualTo("OkButton.0");
        await Assert.That(element.Content).IsEquivalentTo(FirstImage, CollectionOrdering.Matching);
        await Assert.That(element.Threshold).IsEqualTo(0.8m);
        await Assert.That(element.Grayscale).IsTrue();
    }

    [Test]
    public async Task WhenClassIsInGlobalNamespace_GeneratesWithoutNamespace()
    {
        var fileSystem = new FakeFileSystem().AddFile(ImagesDirectory + "/logo.png", FirstImage);
        var harness = new GeneratorHarness(fileSystem);

        var outcome = harness.Run("""[Askaiser.Marionette.ImageLibrary("images")] internal sealed partial class GlobalLibrary { }""");

        await Assert.That(outcome.CompilationProblems).IsEmpty();
        await Assert.That(outcome.Sources.Keys.Single()).IsEqualTo("GlobalLibrary.images.g.cs");
        await Assert.That(outcome.SingleSource).DoesNotContain("namespace");
        await Assert.That(((ImageElement)outcome.Evaluate("GlobalLibrary", "Instance", "Logo")!).Name).IsEqualTo("Logo.0");
    }

    [Test]
    public async Task WhenImagesAreNumbered_GeneratesAnArrayInOrder()
    {
        var fileSystem = new FakeFileSystem()
            .AddFile(ImagesDirectory + "/logo_2.png", ThirdImage)
            .AddFile(ImagesDirectory + "/logo_0.png", FirstImage)
            .AddFile(ImagesDirectory + "/logo_gs_1.png", SecondImage);

        var outcome = new GeneratorHarness(fileSystem).Run(LibrarySource);

        await Assert.That(outcome.CompilationProblems).IsEmpty();

        var elements = ((IElement[])outcome.Evaluate("MyCode.MyLibrary", "Instance", "Logo")!).Cast<ImageElement>().ToArray();
        await Assert.That(elements.Select(element => element.Name)).IsEquivalentTo(["Logo.0", "Logo.1", "Logo.2"], CollectionOrdering.Matching);
        await Assert.That(elements.Select(element => element.Grayscale)).IsEquivalentTo([false, true, false], CollectionOrdering.Matching);
        await Assert.That(elements[2].Content).IsEquivalentTo(ThirdImage, CollectionOrdering.Matching);
    }

    [Test]
    public async Task WhenImagesAreInDirectories_GeneratesNestedLibraries()
    {
        var fileSystem = new FakeFileSystem()
            .AddFile(ImagesDirectory + "/logo.png", FirstImage)
            .AddFile(ImagesDirectory + "/pages/login/title.png", SecondImage)
            .AddFile(ImagesDirectory + "/pages/login--submit.png", ThirdImage)
            .AddFile(ImagesDirectory + "/pages--home--banner.jpg", FirstImage)
            .AddFile(ImagesDirectory + "/empty/readme.txt", FirstImage);

        var outcome = new GeneratorHarness(fileSystem).Run(LibrarySource);

        await Assert.That(outcome.Diagnostics).IsEmpty();
        await Assert.That(outcome.CompilationProblems).IsEmpty();

        await Assert.That(((ImageElement)outcome.Evaluate("MyCode.MyLibrary", "Instance", "Logo")!).Name).IsEqualTo("Logo.0");
        await Assert.That(((ImageElement)outcome.Evaluate("MyCode.MyLibrary", "Instance", "Pages", "Login", "Title")!).Name).IsEqualTo("Pages.Login.Title.0");
        await Assert.That(((ImageElement)outcome.Evaluate("MyCode.MyLibrary", "Instance", "Pages", "Login", "Submit")!).Name).IsEqualTo("Pages.Login.Submit.0");
        await Assert.That(((ImageElement)outcome.Evaluate("MyCode.MyLibrary", "Instance", "Pages", "Home", "Banner")!).Name).IsEqualTo("Pages.Home.Banner.0");

        // Directories without images do not get a property.
        var library = outcome.Evaluate("MyCode.MyLibrary", "Instance")!;
        await Assert.That(library.GetType().GetProperty("Empty")).IsNull();

        // Libraries are nested in the class instead of being added to its namespace.
        await Assert.That(outcome.Evaluate("MyCode.MyLibrary", "Instance", "Pages")!.GetType().FullName).IsEqualTo("MyCode.MyLibrary+PagesLibrary");
    }

    [Test]
    public async Task ElementsAreCreatedOnce_AndSharedByInstances()
    {
        var fileSystem = new FakeFileSystem().AddFile(ImagesDirectory + "/logo.png", FirstImage);
        var outcome = new GeneratorHarness(fileSystem).Run(LibrarySource, """
            namespace MyCode
            {
                public static class Usage
                {
                    public static bool SameElement => ReferenceEquals(MyLibrary.Instance.Logo, new MyLibrary().Logo);

                    public static bool SameInstance => ReferenceEquals(MyLibrary.Instance, MyLibrary.Instance);
                }
            }
            """);

        await Assert.That(outcome.CompilationProblems).IsEmpty();
        await Assert.That((bool)outcome.Evaluate("MyCode.Usage", "SameElement")!).IsTrue();
        await Assert.That((bool)outcome.Evaluate("MyCode.Usage", "SameInstance")!).IsTrue();
    }

    [Test]
    public async Task WhenSeveralLibraries_GeneratesOneFilePerLibrary()
    {
        var fileSystem = new FakeFileSystem()
            .AddFile(GeneratorHarness.SourceDirectory + "/first/buttons/ok.png", FirstImage)
            .AddFile(GeneratorHarness.SourceDirectory + "/second/buttons/ok.png", SecondImage);

        var outcome = new GeneratorHarness(fileSystem).Run("""
            namespace MyCode
            {
                [Askaiser.Marionette.ImageLibrary("first")]
                public partial class FirstLibrary { }

                [Askaiser.Marionette.ImageLibrary("second")]
                public partial class SecondLibrary { }
            }
            """);

        // Both libraries have a "buttons" directory, which used to generate two classes with the same name.
        await Assert.That(outcome.CompilationProblems).IsEmpty();
        await Assert.That(outcome.Sources.Keys.Order()).IsEquivalentTo(["MyCode.FirstLibrary.images.g.cs", "MyCode.SecondLibrary.images.g.cs"], CollectionOrdering.Matching);
        await Assert.That(((ImageElement)outcome.Evaluate("MyCode.SecondLibrary", "Instance", "Buttons", "Ok")!).Content).IsEquivalentTo(SecondImage, CollectionOrdering.Matching);
    }

    [Test]
    public async Task WhenDirectoryIsAbsolute_UsesIt()
    {
        var fileSystem = new FakeFileSystem().AddFile("/elsewhere/shared-images/logo.png", FirstImage);

        var outcome = new GeneratorHarness(fileSystem).Run("""[Askaiser.Marionette.ImageLibrary("/elsewhere/shared-images")] public partial class MyLibrary { }""");

        await Assert.That(outcome.Diagnostics).IsEmpty();
        await Assert.That(outcome.SingleSource).Contains("public global::Askaiser.Marionette.IElement Logo");
    }

    [Test]
    public async Task GeneratedCode_DoesNotDependOnTheOrderFilesAreListedIn()
    {
        static FakeFileSystem CreateFileSystem(bool reversed) => new FakeFileSystem { ListInReverseOrder = reversed }
            .AddFile(ImagesDirectory + "/b.png", FirstImage)
            .AddFile(ImagesDirectory + "/a_1.png", SecondImage)
            .AddFile(ImagesDirectory + "/a_0.png", ThirdImage)
            .AddFile(ImagesDirectory + "/z/y.png", FirstImage)
            .AddFile(ImagesDirectory + "/c/d.png", SecondImage);

        var first = new GeneratorHarness(CreateFileSystem(reversed: false)).Run(LibrarySource);
        var second = new GeneratorHarness(CreateFileSystem(reversed: true)).Run(LibrarySource);

        await Assert.That(second.SingleSource).IsEqualTo(first.SingleSource);
    }

    [Test]
    public async Task GeneratedCode_DescribesImagesInDocumentation()
    {
        // The header of a 112x26 PNG image.
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52, 0, 0, 0, 112, 0, 0, 0, 26, 8, 2, 0, 0, 0];
        var fileSystem = new FakeFileSystem().AddFile(ImagesDirectory + "/toolbar/save_gs.png", png);

        var outcome = new GeneratorHarness(fileSystem).Run(LibrarySource);

        await Assert.That(outcome.SingleSource).Contains("<c>toolbar/save_gs.png</c> (112x26 pixels). Searched in grayscale with a threshold of 0.95.");
    }
}
