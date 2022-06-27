using System;
#if !NETSTANDARD2_0
using System.Buffers;
#endif
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

public static class ElementCollectionExtensions
{
    public static async Task LoadAsync(this ElementCollection elements, Stream stream)
    {
        JsonDocument? jsonDocument = null;

        try
        {
#if NETSTANDARD2_0
            using (stream)
#else
            await using (stream.ConfigureAwait(false))
#endif
            {
                jsonDocument = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            }

            elements.Clear();

            foreach (var jsonObject in jsonDocument.RootElement.EnumerateArray())
            {
                var elementKind = jsonObject.GetProperty("kind").GetString();

                IElement element = elementKind switch
                {
                    JsonElementKinds.Image => new ImageElement(Deserialize<JsonImageElement>(jsonObject)),
                    JsonElementKinds.Text => new TextElement(Deserialize<JsonTextElement>(jsonObject)),
                    null => throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_NullJsonElement),
                    _ => throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_UnsupportedElementType.FormatInvariant(elementKind)),
                };

                if (string.IsNullOrWhiteSpace(element.Name))
                {
                    throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_InvalidElementName);
                }

                if (elements.Contains(element))
                {
                    throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_ElementAlreadyExists.FormatInvariant(element.Name));
                }

                elements.Add(element);
            }
        }
        finally
        {
            jsonDocument?.Dispose();
        }
    }

    public static async Task<ElementCollection> LoadAsync(this ElementCollection elements, string filePath)
    {
        var stream = File.OpenRead(filePath);

#if NETSTANDARD2_0
        using (stream)
#else
        await using (stream.ConfigureAwait(false))
#endif
        {
            await LoadAsync(elements, stream).ConfigureAwait(false);
        }

        return elements;
    }

    public static async Task<ElementCollection> SaveAsync(this ElementCollection elements, Stream destinationStream)
    {
        var jsonElements = new List<object>(elements.Count);

        foreach (var element in elements.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            JsonBaseElement jsonElement = element switch
            {
                ImageElement imageElement => new JsonImageElement(imageElement),
                TextElement textElement => new JsonTextElement(textElement),
                _ => throw new NotSupportedException(Messages.ElementCollectionExtensions_Throw_UnsupportedElementType.FormatInvariant(element.GetType().FullName ?? "unknown")),
            };

            jsonElements.Add(jsonElement);
        }

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

#if NETSTANDARD2_0
        using (destinationStream)
#else
        await using (destinationStream.ConfigureAwait(false))
#endif
        {
            await JsonSerializer.SerializeAsync(destinationStream, jsonElements, jsonSerializerOptions).ConfigureAwait(false);
        }

        return elements;
    }

    public static async Task SaveAsync(this ElementCollection elements, string filePath)
    {
        var destinationStream = File.Create(filePath);

#if NETSTANDARD2_0
        using (destinationStream)
#else
        await using (destinationStream.ConfigureAwait(false))
#endif
        {
            await SaveAsync(elements, destinationStream).ConfigureAwait(false);
        }
    }

    private static T Deserialize<T>(JsonElement element)
    {
#if NETSTANDARD2_0
        var json = element.GetRawText();
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Cound not deserialize JSON element");
#else
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            element.WriteTo(writer);
        }

        return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan) ?? throw new InvalidOperationException("Cound not deserialize JSON element");
#endif
    }
}
