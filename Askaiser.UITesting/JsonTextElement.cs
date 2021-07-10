using System.Text.Json.Serialization;

namespace Askaiser.UITesting
{
    internal sealed class JsonTextElement : JsonBaseElement
    {
        [JsonConstructor]
        public JsonTextElement()
        {
            this.Kind = JsonElementKinds.Text;
        }

        internal JsonTextElement(TextElement element)
            : this()
        {
            this.Name = element.Name;
            this.Content = element.Content;
            this.Options = element.Options;
            this.IgnoreCase = element.IgnoreCase;
        }

        [JsonPropertyName("options")]
        public TextOptions Options { get; set; }

        [JsonPropertyName("ignoreCase")]
        public bool IgnoreCase { get; set; }
    }
}
