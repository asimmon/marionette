using System;
using System.Diagnostics;

namespace Askaiser.UITesting
{
    [DebuggerDisplay("{Name}")]
    public sealed class TextElement : IElement
    {
        private readonly string _name;
        private readonly string _content;

        public TextElement(string name, string content)
        {
            this.Name = name;
            this.Content = content;
        }

        internal TextElement(JsonTextElement json)
        {
            this.Name = json.Name;
            this.Content = json.Content;
        }

        public string Name
        {
            get => this._name;
            init => this._name = value is { Length: > 0 } ? value : throw new ArgumentException("Name cannot be null or empty.", nameof(this.Name));
        }

        public string Content
        {
            get => this._content;
            init => this._content = value is { Length: > 0 } ? value : throw new ArgumentException("Content cannot be null or empty.", nameof(this.Content));
        }
    }
}