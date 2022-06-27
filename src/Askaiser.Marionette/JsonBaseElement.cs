using System.Text.Json.Serialization;

namespace Askaiser.Marionette;

internal abstract class JsonBaseElement
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}
