using System;
using System.Diagnostics;

namespace Askaiser.UITesting
{
    [DebuggerDisplay("{Name}")]
    public sealed class TextElement : IElement
    {
        private readonly string _name;
        private readonly string _content;

        public TextElement(string name, string content, TextOptions options)
        {
            this.Name = name;
            this.Content = content;
            this.Options = options;
            this.IgnoreCase = true;
        }

        public TextElement(string content, TextOptions options = TextOptions.BlackAndWhite)
            : this(Guid.NewGuid().ToString("N"), content, options)
        {
        }

        internal TextElement(JsonTextElement json)
        {
            this.Name = json.Name;
            this.Content = json.Content;
            this.Options = json.Options;
            this.IgnoreCase = json.IgnoreCase;
        }

        public string Name
        {
            get => this._name;
            init => this._name = value?.Trim() is { Length: > 0 } trimmedValue ? trimmedValue : throw new ArgumentException("Name cannot be null or empty.", nameof(this.Name));
        }

        public string Content
        {
            get => this._content;
            init => this._content = value is { Length: > 0 } ? value : throw new ArgumentException("Content cannot be null or empty.", nameof(this.Content));
        }

        public TextOptions Options { get; init; }

        public bool IgnoreCase { get; init; }

        public override string ToString()
        {
            return this.Content;
        }
    }
}
