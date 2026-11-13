using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator.Tests;

/// <summary>
/// A generator runs at every keystroke in an IDE. These tests verify that images are only read when they changed.
/// </summary>
public class ImageLibraryGeneratorIncrementalityTests
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
    private static readonly byte[] OtherImage = [4, 5, 6, 7];

    [Test]
    public async Task WhenUnrelatedCodeChanges_DoesNotReadImagesAgain()
    {
        var fileSystem = new FakeFileSystem()
            .AddFile(ImagesDirectory + "/first.png", Image)
            .AddFile(ImagesDirectory + "/second.png", OtherImage);
        var harness = new GeneratorHarness(fileSystem);

        var first = harness.Run(LibrarySource, "namespace MyCode { public class Other { } }");
        await Assert.That(fileSystem.ReadCount).IsEqualTo(2);

        var second = harness.Run(LibrarySource, "namespace MyCode { public class Other { public int Value { get; set; } } }");

        await Assert.That(fileSystem.ReadCount).IsEqualTo(2);
        await Assert.That(second.OutputReasons.All(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)).IsTrue();
        await Assert.That(second.SingleSource).IsEqualTo(first.SingleSource);
    }

    [Test]
    public async Task WhenTheLibraryClassIsEdited_DoesNotReadImagesAgain()
    {
        var fileSystem = new FakeFileSystem().AddFile(ImagesDirectory + "/first.png", Image);
        var harness = new GeneratorHarness(fileSystem);

        harness.Run(LibrarySource);
        harness.Run(LibrarySource.Replace("{\n    }", "{\n        public int Value { get; set; }\n    }", StringComparison.Ordinal).Replace("{\r\n    }", "{\r\n        public int Value { get; set; }\r\n    }", StringComparison.Ordinal));

        await Assert.That(fileSystem.ReadCount).IsEqualTo(1);
    }

    [Test]
    public async Task WhenAnImageIsModified_GeneratesAgain()
    {
        var fileSystem = new FakeFileSystem().AddFile(ImagesDirectory + "/first.png", Image, lastWriteTicks: 1);
        var harness = new GeneratorHarness(fileSystem);

        var first = harness.Run(LibrarySource);

        // Same name and same size, only the modification time differs.
        fileSystem.AddFile(ImagesDirectory + "/first.png", [3, 2, 1], lastWriteTicks: 2);
        var second = harness.Run(LibrarySource);

        await Assert.That(fileSystem.ReadCount).IsEqualTo(2);
        await Assert.That(second.SingleSource).IsNotEqualTo(first.SingleSource);
        await Assert.That(((ImageElement)second.Evaluate("MyCode.MyLibrary", "Instance", "First")!).Content).IsEquivalentTo(new byte[] { 3, 2, 1 });
    }

    [Test]
    public async Task WhenAnImageIsAdded_GeneratesItsProperty()
    {
        var fileSystem = new FakeFileSystem().AddFile(ImagesDirectory + "/first.png", Image);
        var harness = new GeneratorHarness(fileSystem);

        var first = harness.Run(LibrarySource);
        await Assert.That(first.SingleSource).DoesNotContain(" Second ");

        fileSystem.AddFile(ImagesDirectory + "/second.png", OtherImage);
        var second = harness.Run(LibrarySource);

        await Assert.That(second.SingleSource).Contains(" Second ");
        await Assert.That(second.CompilationProblems).IsEmpty();
    }

    [Test]
    public async Task WhenAFileThatIsNotAnImageChanges_DoesNotGenerateAgain()
    {
        var fileSystem = new FakeFileSystem().AddFile(ImagesDirectory + "/first.png", Image);
        var harness = new GeneratorHarness(fileSystem);

        harness.Run(LibrarySource);
        fileSystem.AddFile(ImagesDirectory + "/notes.txt", OtherImage);
        var second = harness.Run(LibrarySource);

        await Assert.That(fileSystem.ReadCount).IsEqualTo(1);
        await Assert.That(second.OutputReasons.All(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)).IsTrue();
    }
}
