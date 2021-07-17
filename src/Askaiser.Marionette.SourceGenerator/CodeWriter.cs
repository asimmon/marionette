using System;
using System.Text;

namespace Askaiser.Marionette.SourceGenerator
{
    internal sealed class CodeWriter
    {
        private readonly StringBuilder _sb;
        private string _indentation;
        private bool _isAtBeginningOfNewLine;

        public CodeWriter()
        {
            this._sb = new StringBuilder();
            this._indentation = string.Empty;
            this._isAtBeginningOfNewLine = true;
        }

        public CodeWriter Append(string text)
        {
            if (text is null)
            {
                throw new ArgumentException(nameof(text));
            }

            if (text.Length == 0)
            {
                return this;
            }

            if (this._isAtBeginningOfNewLine)
            {
                this._sb.Append(this._indentation);
                this._isAtBeginningOfNewLine = false;
            }

            this._sb.Append(text);
            return this;
        }

        public CodeWriter AppendLine() => this.AppendLine(string.Empty);

        public CodeWriter AppendLine(string text)
        {
            this.Append(text);
            this._sb.AppendLine();
            this._isAtBeginningOfNewLine = true;
            return this;
        }

        public IDisposable BeginBlock()
        {
            return new CodeBlock(this);
        }

        public IDisposable BeginClass(string modifier, string name, string inherits = null)
        {
            if ((modifier = modifier?.Trim()) is not { Length: > 0 })
            {
                throw new ArgumentException(nameof(modifier));
            }

            if ((name = name?.Trim()) is not { Length: > 0 })
            {
                throw new ArgumentException(nameof(name));
            }

            this.Append(modifier).Append(" class ").Append(name);

            if (!string.IsNullOrEmpty(inherits))
            {
                this.Append(" : ").Append(inherits);
            }

            this.AppendLine();

            return new CodeBlock(this);
        }

        public IDisposable BeginNamespace(string name)
        {
            if ((name = name?.Trim()) is not { Length: > 0 })
            {
                throw new ArgumentException(nameof(name));
            }

            this.Append("namespace ").AppendLine(name);
            return new CodeBlock(this);
        }

        public override string ToString()
        {
            return this._sb.ToString();
        }

        private void IncreaseIndentation()
        {
            this._indentation = new string(' ', this._indentation.Length + 4);
        }

        private void DecreaseIndentation()
        {
            this._indentation = new string(' ', Math.Max(0, this._indentation.Length - 4));
        }

        private sealed class CodeBlock : IDisposable
        {
            private readonly CodeWriter _writer;

            public CodeBlock(CodeWriter writer)
            {
                this._writer = writer;
                this._writer.AppendLine("{");
                this._writer.IncreaseIndentation();
            }

            public void Dispose()
            {
                this._writer.DecreaseIndentation();
                this._writer.AppendLine("}");
            }
        }
    }
}
