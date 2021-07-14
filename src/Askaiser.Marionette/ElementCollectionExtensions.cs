using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Askaiser.Marionette
{
    public static class ElementCollectionExtensions
    {
        public static async Task LoadAsync(this ElementCollection elements, Stream stream)
        {
            JsonDocument jsonDocument = null;

            try
            {
                await using (stream.ConfigureAwait(false))
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
                        null => throw new ArgumentException("The JSON document cannot null elements."),
                        _ => throw new ArgumentException($"The JSON document contains an unsupported element type: '{elementKind}'.")
                    };

                    if (string.IsNullOrWhiteSpace(element.Name))
                        throw new ArgumentException("An element cannot have a null or empty name.");

                    if (elements.Contains(element))
                        throw new ArgumentException($"An element with the name '{element.Name}' already exists.");

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
            await using (stream.ConfigureAwait(false))
                await LoadAsync(elements, stream).ConfigureAwait(false);
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
                    _ => throw new NotSupportedException($"Element of type {element.GetType().FullName} is not supported")
                };

                jsonElements.Add(jsonElement);
            }

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            await using (destinationStream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(destinationStream, jsonElements, jsonSerializerOptions).ConfigureAwait(false);
            }

            return elements;
        }

        public static async Task SaveAsync(this ElementCollection elements, string filePath)
        {
            var destinationStream = File.Create(filePath);
            await using (destinationStream.ConfigureAwait(false))
                await SaveAsync(elements, destinationStream).ConfigureAwait(false);
        }

        private static T Deserialize<T>(JsonElement element)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
                element.WriteTo(writer);
            return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan);
        }
    }
}
