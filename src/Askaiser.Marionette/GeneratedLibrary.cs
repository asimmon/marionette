using System;
using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Marionette
{
    internal class GeneratedLibrary
    {
        public GeneratedLibrary(string name)
            : this(name, null)
        {
        }

        private GeneratedLibrary(string name, GeneratedLibrary parent)
        {
            this.Name = name.ToPascalCasedPropertyName();
            this.Level = parent?.Level + 1 ?? 0;
            this.Parent = parent;
            this.Libraries = new Dictionary<string, GeneratedLibrary>(StringComparer.OrdinalIgnoreCase);
            this.Images = new Dictionary<string, List<GeneratedImage>>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }

        public int Level { get; }

        public string UniqueName
        {
            get => string.Join("", this.GetHierarchy().Reverse().Select(x => x.Name));
        }

        private GeneratedLibrary Parent { get; }

        public Dictionary<string, GeneratedLibrary> Libraries { get; }

        public Dictionary<string, List<GeneratedImage>> Images { get; }

        public GeneratedLibrary CreateChild(string name)
        {
            return new GeneratedLibrary(name, this);
        }

        public IEnumerable<GeneratedLibrary> GetHierarchy()
        {
            yield return this;

            for (var parent = this.Parent; parent != null; parent = parent.Parent)
            {
                yield return parent;
            }
        }

        public IEnumerable<GeneratedImage> GetImagesChildren()
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
