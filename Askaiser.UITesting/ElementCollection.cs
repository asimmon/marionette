using System;
using System.Collections.Generic;
using System.Linq;

namespace Askaiser.UITesting
{
    public sealed class ElementCollection : HashSet<IElement>
    {
        public ElementCollection()
            : base(ElementComparer.Instance)
        {
        }

        public IElement this[string name]
        {
            get => this.First(x => name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
        }

        public bool TryGetValue(string name, out IElement value)
        {
            value = this.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            return value != null;
        }

        private sealed class ElementComparer: IEqualityComparer<IElement>
        {
            public static readonly ElementComparer Instance = new ElementComparer();

            public bool Equals(IElement x, IElement y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                return x.Name == y.Name;
            }

            public int GetHashCode(IElement obj)
            {
                return obj.Name != null ? obj.Name.GetHashCode() : 0;
            }
        }
    }
}