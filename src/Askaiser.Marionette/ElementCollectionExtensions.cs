using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

/// <summary>
/// Saves and loads element collections as JSON documents.
/// </summary>
public static class ElementCollectionExtensions
{
    private const string ImageKind = "image";
    private const string TextKind = "text";
    private const int FileBufferSize = 4096;

    /// <summary>
    /// Replaces the content of a collection with the elements of a JSON document, as written by <see cref="SaveAsync(ElementCollection, Stream, CancellationToken)"/>.
    /// </summary>
    /// <param name="elements">The collection to fill.</param>
    /// <param name="stream">The stream to read the JSON document from. It is closed once read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentException">The document contains an element that has no name, the same name as another one, or an unknown kind.</exception>
    /// <exception cref="JsonException">The stream does not contain a valid JSON document.</exception>
    public static async Task LoadAsync(this ElementCollection elements, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(stream);

        JsonDocument jsonDocument;
        await using (stream.ConfigureAwait(false))
        {
            jsonDocument = await JsonDocument.ParseAsync(stream, default, cancellationToken).ConfigureAwait(false);
        }

        using (jsonDocument)
        {
            elements.Clear();

            foreach (var jsonObject in jsonDocument.RootElement.EnumerateArray())
            {
                if (jsonObject.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_NullJsonElement, nameof(stream));
                }

                var kind = GetString(jsonObject, "kind");
                var name = GetString(jsonObject, "name") ?? string.Empty;
                var content = GetString(jsonObject, "content") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_InvalidElementName, nameof(stream));
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
                    null => throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_NullJsonElement, nameof(stream)),
                    _ => throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_UnsupportedElementType.FormatInvariant(kind), nameof(stream)),
                };

                if (elements.Contains(element))
                {
                    throw new ArgumentException(Messages.ElementCollectionExtensions_Throw_ElementAlreadyExists.FormatInvariant(element.Name), nameof(stream));
                }

                elements.Add(element);
            }
        }
    }

    /// <summary>
    /// Replaces the content of a collection with the elements of a JSON file, as written by <see cref="SaveAsync(ElementCollection, string, CancellationToken)"/>.
    /// </summary>
    /// <param name="elements">The collection to fill.</param>
    /// <param name="filePath">The path of the JSON file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The collection, to chain calls.</returns>
    /// <exception cref="ArgumentException">The document contains an element that has no name, the same name as another one, or an unknown kind.</exception>
    /// <exception cref="JsonException">The file does not contain a valid JSON document.</exception>
    public static async Task<ElementCollection> LoadAsync(this ElementCollection elements, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await LoadAsync(elements, stream, cancellationToken).ConfigureAwait(false);
        return elements;
    }

    /// <summary>
    /// Writes the image and text elements of a collection to a stream as a JSON document, sorted by name.
    /// </summary>
    /// <param name="elements">The collection to save.</param>
    /// <param name="destinationStream">The stream to write to. It is closed once written.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The collection, to chain calls.</returns>
    /// <exception cref="NotSupportedException">The collection contains an element that is neither an <see cref="ImageElement"/> nor a <see cref="TextElement"/>.</exception>
    public static async Task<ElementCollection> SaveAsync(this ElementCollection elements, Stream destinationStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(destinationStream);

        await using (destinationStream.ConfigureAwait(false))
        {
            var writer = new Utf8JsonWriter(destinationStream, new JsonWriterOptions { Indented = true, NewLine = "\n" });
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

                    // Images are large: send what was written to the stream as it goes rather than buffering the whole document.
                    if (writer.BytesPending > FileBufferSize)
                    {
                        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                writer.WriteEndArray();
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return elements;
    }

    /// <summary>
    /// Writes the image and text elements of a collection to a JSON file, sorted by name. The file is overwritten when it exists.
    /// </summary>
    /// <param name="elements">The collection to save.</param>
    /// <param name="filePath">The path of the JSON file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="NotSupportedException">The collection contains an element that is neither an <see cref="ImageElement"/> nor a <see cref="TextElement"/>.</exception>
    public static async Task SaveAsync(this ElementCollection elements, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, FileBufferSize, FileOptions.Asynchronous);
        await SaveAsync(elements, stream, cancellationToken).ConfigureAwait(false);
    }

    private static string? GetString(JsonElement jsonObject, string propertyName)
    {
        return jsonObject.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }
}
