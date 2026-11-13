namespace Askaiser.Marionette.SourceGenerator.Tests;

public class ImageLibraryGeneratorDiagnosticsTests
{
    private const string ImagesDirectory = GeneratorHarness.SourceDirectory + "/images";

    private const string LibrarySource = """
        namespace MyCode
        {
            [Askaiser.Marionette.ImageLibrary("images")]
            public partial class MyLibrary
            {
            }
        }
        """;

    private static readonly byte[] Image = [1, 2, 3];

    [Test]
    [Arguments("""[Askaiser.Marionette.ImageLibrary("images")] public class MyLibrary { }""", "AMSG002")]
    [Arguments("""[Askaiser.Marionette.ImageLibrary("images")] public static partial class MyLibrary { }""", "AMSG007")]
    [Arguments("""public partial class Outer { [Askaiser.Marionette.ImageLibrary("images")] public partial class MyLibrary { } }""", "AMSG006")]
    [Arguments("""[Askaiser.Marionette.ImageLibrary("images")] public partial class MyLibrary<T> { }""", "AMSG006")]
    [Arguments("""[Askaiser.Marionette.ImageLibrary(null)] public partial class MyLibrary { }""", "AMSG003")]
    [Arguments("""[Askaiser.Marionette.ImageLibrary("")] public partial class MyLibrary { }""", "AMSG003")]
    [Arguments("""[Askaiser.Marionette.ImageLibrary("   ")] public partial class MyLibrary { }""", "AMSG003")]
    public async Task WhenClassCannotBeALibrary_ReportsAndGeneratesNothing(string source, string expectedDiagnostic)
    {
        var harness = new GeneratorHarness(new FakeFileSystem().AddFile(ImagesDirectory + "/logo.png", Image));

        var outcome = harness.Run(source);

        await Assert.That(outcome.Diagnostics.Select(diagnostic => diagnostic.Id)).IsEquivalentTo([expectedDiagnostic]);
        await Assert.That(outcome.Sources).IsEmpty();
    }

    [Test]
    public async Task WhenDirectoryDoesNotExist_ReportsAndGeneratesAnEmptyLibrary()
    {
        var outcome = new GeneratorHarness().Run(LibrarySource);

        var diagnostic = outcome.Diagnostics.Single();
        await Assert.That(diagnostic.Id).IsEqualTo("AMSG008");
        await Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Contains("images");

        // Code that uses the library keeps compiling, which lets the warning be the only thing to fix.
        await Assert.That(outcome.CompilationProblems.Where(problem => problem.Id != "AMSG008")).IsEmpty();
        await Assert.That(outcome.Evaluate("MyCode.MyLibrary", "Instance")).IsNotNull();
    }

    [Test]
    public async Task WhenImageIsTooLarge_ReportsAndIgnoresIt()
    {
        var fileSystem = new FakeFileSystem()
            .AddFile(ImagesDirectory + "/small.png", Image)
            .AddFile(ImagesDirectory + "/huge.png", Image, reportedLength: (2 * 1024 * 1024) + 1);

        var outcome = new GeneratorHarness(fileSystem).Run(LibrarySource);

        await Assert.That(outcome.Diagnostics.Select(diagnostic => diagnostic.Id)).IsEquivalentTo(["AMSG004"]);
        await Assert.That(outcome.SingleSource).Contains(" Small ");
        await Assert.That(outcome.SingleSource).DoesNotContain(" Huge ");
        await Assert.That(fileSystem.ReadCount).IsEqualTo(1);
    }

    [Test]
    public async Task WhenTwoFilesHaveTheSameName_ReportsAndKeepsTheFirst()
    {
        var fileSystem = new FakeFileSystem()
            .AddFile(ImagesDirectory + "/ok-button.png", Image)
            .AddFile(ImagesDirectory + "/ok-button.jpg", Image)
            .AddFile(ImagesDirectory + "/pages--title.png", Image)
            .AddFile(ImagesDirectory + "/pages/title_gs.png", Image);

        var outcome = new GeneratorHarness(fileSystem).Run(LibrarySource);

        await Assert.That(outcome.Diagnostics.Select(diagnostic => diagnostic.Id)).IsEquivalentTo(["AMSG005", "AMSG005"]);
        await Assert.That(outcome.CompilationProblems.Where(problem => problem.Id != "AMSG005")).IsEmpty();
    }

    [Test]
    [Arguments("my-library.png")]
    [Arguments("instance.png")]
    [Arguments("pages.png", "pages/title.png")]
    [Arguments("pages-library.png", "pages/title.png")]
    [Arguments("_.png")]
    [Arguments("pages/pages-library/title.png")]
    public async Task WhenNameCannotBeUsed_ReportsAndStillCompiles(params string[] relativePaths)
    {
        var fileSystem = new FakeFileSystem();
        foreach (var relativePath in relativePaths)
        {
            fileSystem.AddFile(ImagesDirectory + "/" + relativePath, Image);
        }

        var outcome = new GeneratorHarness(fileSystem).Run(LibrarySource);

        await Assert.That(outcome.Diagnostics.Select(diagnostic => diagnostic.Id)).IsEquivalentTo(["AMSG009"]);
        await Assert.That(outcome.CompilationProblems.Where(problem => problem.Id != "AMSG009")).IsEmpty();
    }

    [Test]
    public async Task EmptyAndUnsupportedFiles_AreIgnoredSilently()
    {
        var fileSystem = new FakeFileSystem()
            .AddFile(ImagesDirectory + "/empty.png", [])
            .AddFile(ImagesDirectory + "/notes.txt", Image)
            .AddFile(ImagesDirectory + "/animation.gif", Image)
            .AddFile(ImagesDirectory + "/sub/only-text.md", Image);

        var outcome = new GeneratorHarness(fileSystem).Run(LibrarySource);

        await Assert.That(outcome.Diagnostics).IsEmpty();
        await Assert.That(outcome.CompilationProblems).IsEmpty();
        await Assert.That(outcome.SingleSource).DoesNotContain("IElement ");
    }
}
