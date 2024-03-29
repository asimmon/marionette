﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Askaiser.Marionette.Tests;

public class ElementCollectionTests
{
    private const string ValidJsonCollection = @"[
  {
    ""threshold"": 0.9,
    ""grayscale"": false,
    ""kind"": ""image"",
    ""name"": ""logo"",
    ""content"": ""SGVsbG8=""
  },
  {
    ""options"": 3,
    ""ignoreCase"": true,
    ""kind"": ""text"",
    ""name"": ""lorem"",
    ""content"": ""Lorem ipsum""
  }
]";

    private const string InvalidJsonCollection = @"[{""kind"": ""unknown"", ""name"": ""yolo""}]";

    private static readonly ImageElement ImageElement = new ImageElement("logo", Encoding.UTF8.GetBytes("Hello"), 0.9m, false);
    private static readonly TextElement TextElement = new TextElement("lorem", "Lorem ipsum", TextOptions.BlackAndWhite);

    [Fact]
    public void TryGetValue_WhenExists_ReturnsTrue()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);

        Assert.True(collection.TryGetValue(ImageElement.Name, out var imageElement));
        Assert.Same(ImageElement, imageElement);
    }

    [Fact]
    public void TryGetValue_WhenDoesNotExists_ReturnsTrue()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);

        Assert.False(collection.TryGetValue(TextElement.Name, out _));
    }

    [Fact]
    public void GetIndexer_WhenDoesNotExists_Throws()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);

        Assert.Same(ImageElement, collection[ImageElement.Name]);
    }

    [Fact]
    public void GetIndexer_WhenExists_Returns()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);

        Assert.Throws<ArgumentException>(() => collection[TextElement.Name]);
    }

    [Fact]
    public async Task SaveAsync_WhenSupportedElements_Works()
    {
        var collection = new ElementCollection();
        collection.Add(ImageElement);
        collection.Add(TextElement);

        using var stream = new MemoryStream();
        await collection.SaveAsync(stream);

        var actualJson = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(ValidJsonCollection, actualJson);
    }

    [Fact]
    public async Task LoadAsync_WhenSupportedElements_Works()
    {
        using (var stream = new MemoryStream())
        using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(ValidJsonCollection);
            await writer.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);

            var collection = new ElementCollection();
            await collection.LoadAsync(stream);

            Assert.Equal(2, collection.Count);
            var imageBase = Assert.Single(collection, x => x.Name == ImageElement.Name);
            var image = Assert.IsType<ImageElement>(imageBase);
            Assert.NotSame(ImageElement, image);

            var textBase = Assert.Single(collection, x => x.Name == TextElement.Name);
            var text = Assert.IsType<TextElement>(textBase);
            Assert.NotSame(TextElement, text);

            Assert.Equal(ImageElement.Content, image.Content);
            Assert.Equal(ImageElement.Threshold, image.Threshold);
            Assert.Equal(ImageElement.Grayscale, image.Grayscale);

            Assert.Equal(TextElement.Content, text.Content);
            Assert.Equal(TextElement.Options, text.Options);
            Assert.Equal(TextElement.IgnoreCase, text.IgnoreCase);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenNotSupportedElements_Throws()
    {
        var collection = new ElementCollection();
        collection.Add(new UnknownElement("yolo"));

        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<NotSupportedException>(async () => await collection.SaveAsync(stream));
    }

    [Fact]
    public async Task LoadAsync_WhenNotSupportedElements_Works()
    {
        using (var stream = new MemoryStream())
        using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(InvalidJsonCollection);
            await writer.FlushAsync();
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
