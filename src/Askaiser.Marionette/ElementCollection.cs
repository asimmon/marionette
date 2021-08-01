using System;
using System.Collections;
using System.Collections.Generic;

namespace Askaiser.Marionette
{
    public sealed class ElementCollection : IEnumerable<IElement>
    {
        private readonly Dictionary<string, IElement> _elements;

        public ElementCollection()
        {
            this._elements = new Dictionary<string, IElement>(StringComparer.OrdinalIgnoreCase);
        }

        public int Count
        {
            get => this._elements.Count;
        }

        public void Add(IElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            this._elements.Add(element.Name, element);
        }

        public void Remove(IElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            this._elements.Remove(element.Name);
        }

        public void Remove(string name)
        {
            this._elements.Remove(name);
        }

        public bool Contains(IElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return this.TryGetValue(element.Name, out _);
        }

        public bool Contains(string name)
        {
            return this.TryGetValue(name, out _);
        }

        public void Clear()
        {
            this._elements.Clear();
        }

        public IElement this[string name]
        {
            get => this.TryGetValue(name, out var element) ? element : throw new ArgumentException(Messages.Element_Throw_ElementNotFound.FormatInvariant(name), nameof(name));
        }

        public bool TryGetValue(string name, out IElement value)
        {
            return this._elements.TryGetValue(name, out value);
        }

        public IEnumerator<IElement> GetEnumerator()
        {
            return this._elements.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
