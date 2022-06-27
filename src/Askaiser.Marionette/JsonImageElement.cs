using System;
using System.Text.Json.Serialization;

namespace Askaiser.Marionette;

internal sealed class JsonImageElement : JsonBaseElement
{
    [JsonConstructor]
    public JsonImageElement()
    {
        this.Kind = JsonElementKinds.Image;
    }

    internal JsonImageElement(ImageElement element)
        : this()
    {
        this.Name = element.Name;
        this.Content = Convert.ToBase64String(element.Content, Base64FormattingOptions.None);
        this.Threshold = element.Threshold;
        this.Grayscale = element.Grayscale;
    }

    [JsonPropertyName("threshold")]
    public decimal Threshold { get; set; }

    [JsonPropertyName("grayscale")]
    public bool Grayscale { get; set; }
}
