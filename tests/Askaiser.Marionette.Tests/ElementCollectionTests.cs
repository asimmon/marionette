using System.Text;

namespace Askaiser.Marionette.Tests;

public class ElementCollectionTests
{
    private const string ValidJsonCollection = "[\n  {\n    \"kind\": \"image\",\n    \"name\": \"logo\",\n    \"content\": \"SGVsbG8=\",\n    \"threshold\": 0.9,\n    \"grayscale\": false\n  },\n"
        + "  {\n    \"kind\": \"text\",\n    \"name\": \"lorem\",\n    \"content\": \"Lorem ipsum\",\n    \"options\": 3,\n    \"ignoreCase\": true\n  }\n]";

    private const string InvalidJsonCollection = @"[{""kind"": ""unknown"", ""name"": ""yolo""}]";

    private static readonly ImageElement ImageElement = new ImageElement("logo", Encoding.UTF8.GetBytes("Hello"), 0.9m, false);
    private static readonly TextElement TextElement = new TextElement("lorem", "Lorem ipsum", TextOptions.BlackAndWhite);

    [Test]
    public async Task TryGetValue_WhenExists_ReturnsTrue()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);

        await Assert.That(collection.TryGetValue(ImageElement.Name, out var imageElement)).IsTrue();
        await Assert.That(imageElement).IsSameReferenceAs(ImageElement);
    }

    [Test]
    public async Task TryGetValue_WhenDoesNotExists_ReturnsTrue()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);

        await Assert.That(collection.TryGetValue(TextElement.Name, out _)).IsFalse();
    }

    [Test]
    public async Task GetIndexer_WhenDoesNotExists_Throws()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);

        await Assert.That(collection[ImageElement.Name]).IsSameReferenceAs(ImageElement);
    }

    [Test]
    public async Task GetIndexer_WhenExists_Returns()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);

        Assert.Throws<ArgumentException>(() => _ = collection[TextElement.Name]);
    }

    [Test]
    public async Task SaveAsync_WhenSupportedElements_Works()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);
        collection.Add(TextElement);

        using var stream = new MemoryStream();
        await collection.SaveAsync(stream);

        var actualJson = Encoding.UTF8.GetString(stream.ToArray());
        await Assert.That(actualJson).IsEqualTo(ValidJsonCollection);
    }

    [Test]
    public async Task LoadAsync_WhenSupportedElements_Works()
    {
        using (var stream = new MemoryStream())
        using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(ValidJsonCollection);
            await writer.FlushAsync(TestContext.Current!.Execution.CancellationToken);
            stream.Seek(0, SeekOrigin.Begin);

            var collection = new ElementCollection();
            await collection.LoadAsync(stream);

            await Assert.That(collection.Count).IsEqualTo(2);
            var imageBase = collection.Single(x => x.Name == ImageElement.Name);
            var image = (await Assert.That(imageBase).IsTypeOf<ImageElement>())!;
            await Assert.That(image).IsNotSameReferenceAs(ImageElement);

            var textBase = collection.Single(x => x.Name == TextElement.Name);
            var text = (await Assert.That(textBase).IsTypeOf<TextElement>())!;
            await Assert.That(text).IsNotSameReferenceAs(TextElement);

            await Assert.That(image.Content.AsSpan().SequenceEqual(ImageElement.Content)).IsTrue();
            await Assert.That(image.Threshold).IsEqualTo(ImageElement.Threshold);
            await Assert.That(image.Grayscale).IsEqualTo(ImageElement.Grayscale);

            await Assert.That(text.Content).IsEqualTo(TextElement.Content);
            await Assert.That(text.Options).IsEqualTo(TextElement.Options);
            await Assert.That(text.IgnoreCase).IsEqualTo(TextElement.IgnoreCase);
        }
    }

    [Test]
    public async Task SaveAsync_WhenNotSupportedElements_Throws()
    {
        var collection = new ElementCollection();
        collection.Add(new UnknownElement("yolo"));

        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<NotSupportedException>(async () => await collection.SaveAsync(stream));
    }

    [Test]
    public async Task LoadAsync_WhenNotSupportedElements_Works()
    {
        using (var stream = new MemoryStream())
        using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(InvalidJsonCollection);
            await writer.FlushAsync(TestContext.Current!.Execution.CancellationToken);
            stream.Seek(0, SeekOrigin.Begin);

            var collection = new ElementCollection();
            await Assert.ThrowsAsync<ArgumentException>(async () => await collection.LoadAsync(stream));
        }
    }

    private sealed class UnknownElement : IElement
    {
        public UnknownElement(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}
