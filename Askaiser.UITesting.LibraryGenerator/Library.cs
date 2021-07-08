using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Askaiser.UITesting.LibraryGenerator
{
    [DebuggerDisplay("{Name} - {Images.Count}")]
    internal class Library
    {
        public Library(string name)
            : this(name, parent: null)
        {
        }

        private Library(string name, Library parent)
        {
            this.Name = name.ToPascalCasedPropertyName();
            this.Level = parent?.Level + 1 ?? 0;
            this.Parent = parent;
            this.Libraries = new Dictionary<string, Library>(StringComparer.OrdinalIgnoreCase);
            this.Images = new Dictionary<string, List<Image>>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }

        public int Level { get; }

        public string UniqueName
        {
            get => string.Join("", this.GetHierarchy().Select(x => x.Name));
        }

        public Library Parent { get; }

        public Dictionary<string, Library> Libraries { get; }

        public Dictionary<string, List<Image>> Images { get; }

        public Library CreateChild(string name)
        {
            return new Library(name, this);
        }

        public IEnumerable<Library> GetHierarchy()
        {
            for (var parent = this.Parent; parent != null; parent = parent.Parent)
            {
                yield return parent;
            }

            yield return this;
        }

        public IEnumerable<Image> GetImagesChildren()
        {
            foreach (var imageGroup in this.Images.Values)
            foreach (var image in imageGroup.OrderBy(x => x.GroupIndex))
                yield return image;

            foreach (var library in this.Libraries.Values)
            foreach (var image in library.GetImagesChildren())
                yield return image;
        }
    }
}