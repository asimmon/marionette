using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

/// <summary>
/// Saves and loads element collections as JSON documents.
/// </summary>
public static class ElementCollectionExtensions
{
    private const string ImageKind = "image";
    private const string TextKind = "text";

    public static async Task LoadAsync(this ElementCollection elements, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(stream);

        JsonDocument jsonDocument;
        await using (stream.ConfigureAwait(false))
        {
            jsonDocument = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
        }

        using (jsonDocument)
        {
            elements.Clear();

            foreach (var jsonObject in jsonDocument.RootElement.EnumerateArray())
            {
                if (jsonObject.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_NullJsonElement);
                }

                var kind = GetString(jsonObject, "kind");
                var name = GetString(jsonObject, "name") ?? string.Empty;
                var content = GetString(jsonObject, "content") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_InvalidElementName);
                }

                IElement element = kind switch
                {
                    ImageKind => new ImageElement(
                        name,
                        Convert.FromBase64String(content),
                        jsonObject.TryGetProperty("threshold", out var threshold) ? threshold.GetDecimal() : ImageElement.DefaultThreshold,
                        jsonObject.TryGetProperty("grayscale", out var grayscale) && grayscale.GetBoolean()),
                    TextKind => new TextElement(name, content, jsonObject.TryGetProperty("options", out var options) ? (TextOptions)options.GetInt32() : TextOptions.None)
                    {
                        IgnoreCase = !jsonObject.TryGetProperty("ignoreCase", out var ignoreCase) || ignoreCase.GetBoolean(),
                    },
                    null => throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_NullJsonElement),
                    _ => throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_UnsupportedElementType.FormatInvariant(kind)),
                };

                if (elements.Contains(element))
                {
                    throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_ElementAlreadyExists.FormatInvariant(element.Name));
                }

                elements.Add(element);
            }
        }
    }

    public static async Task<ElementCollection> LoadAsync(this ElementCollection elements, string filePath)
    {
        await LoadAsync(elements, File.OpenRead(filePath)).ConfigureAwait(false);
        return elements;
    }

    public static async Task<ElementCollection> SaveAsync(this ElementCollection elements, Stream destinationStream)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(destinationStream);

        await using (destinationStream.ConfigureAwait(false))
        {
            var writer = new Utf8JsonWriter(destinationStream, new JsonWriterOptions { Indented = true });
            await using (writer.ConfigureAwait(false))
            {
                writer.WriteStartArray();

                foreach (var element in elements.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    writer.WriteStartObject();

                    switch (element)
                    {
                        case ImageElement image:
                            writer.WriteString("kind", ImageKind);
                            writer.WriteString("name", image.Name);
                            writer.WriteBase64String("content", image.Content);
                            writer.WriteNumber("threshold", image.Threshold);
                            writer.WriteBoolean("grayscale", image.Grayscale);
                            break;
                        case TextElement text:
                            writer.WriteString("kind", TextKind);
                            writer.WriteString("name", text.Name);
                            writer.WriteString("content", text.Content);
                            writer.WriteNumber("options", (int)text.Options);
                            writer.WriteBoolean("ignoreCase", text.IgnoreCase);
                            break;
                        default:
                            throw new NotSupportedException(Messages.ElementCollectionExtensions_Throw_UnsupportedElementType.FormatInvariant(element.GetType().FullName ?? "unknown"));
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        return elements;
    }

    public static async Task SaveAsync(this ElementCollection elements, string filePath)
    {
        await SaveAsync(elements, File.Create(filePath)).ConfigureAwait(false);
    }

    private static string? GetString(JsonElement jsonObject, string propertyName)
    {
        return jsonObject.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }
}
