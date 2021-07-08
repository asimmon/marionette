using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Askaiser.UITesting
{
    internal class GeneratedImage
    {
        // Threshold can only match 1.0 or 0.X where X is one to four digits
        private static readonly Regex ThresholdRegex = new Regex("^(1\\.0|0\\.[0-9]{1,4})$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex GroupIndexRegex = new Regex("^[1-9][0-9]*$", RegexOptions.Compiled | RegexOptions.Singleline);

        private GeneratedImage(string fileName, byte[] bytes, GeneratedLibrary rootLibrary)
        {
            var libsAndElementRawNames = Path.GetFileNameWithoutExtension(fileName).Split("--", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var librariesRawNames = libsAndElementRawNames.SkipLast(1).ToArray();

            var elementRawName = libsAndElementRawNames[^1];
            var elementNameParts = elementRawName.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            this.Name = elementNameParts[0].ToPascalCasedPropertyName();
            this.Bytes = bytes;
            this.Threshold = 0.95m;
            this.Grayscale = false;
            this.GroupIndex = 0;

            if (elementNameParts.Length > 1)
            {
                for (var i = 1; i < elementNameParts.Length; i++)
                {
                    var namePart = elementNameParts[i];

                    if ("gs".Equals(namePart, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Grayscale = true;
                    }
                    else if (ThresholdRegex.Match(namePart) is { Success: true } thresholdMatch)
                    {
                        this.Threshold = decimal.Parse(thresholdMatch.Groups[0].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                    }
                }

                if (GroupIndexRegex.Match(elementNameParts[^1]) is { Success: true } groupIndexMatch)
                {
                    this.GroupIndex = int.Parse(groupIndexMatch.Groups[0].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
            }

            this.Parent = EnsureLibraryHierarchy(rootLibrary, librariesRawNames);
        }

        public string Name { get; }

        public byte[] Bytes { get; }

        public decimal Threshold { get; }

        public bool Grayscale { get; }

        public int GroupIndex { get; }

        public GeneratedLibrary Parent { get; }

        public string UniqueName
        {
            get => string.Join('.', this.GetUniqueNameParts());
        }

        public static GeneratedImage Create(FileInfo imageFile, GeneratedLibrary rootLibrary)
        {
            byte[] bytes;
            using (var srcBitmap = System.Drawing.Image.FromFile(imageFile.FullName))
            using (var dstStream = new MemoryStream())
            {
                srcBitmap.Save(dstStream, ImageFormat.Png);
                bytes = dstStream.ToArray();
            }

            return new GeneratedImage(imageFile.Name, bytes, rootLibrary);
        }

        private static GeneratedLibrary EnsureLibraryHierarchy(GeneratedLibrary library, IEnumerable<string> librariesRawNames)
        {
            foreach (var libName in librariesRawNames)
            {
                var localLibraryRef = library;
                library = library.Libraries.GetOrCreate(libName, x => localLibraryRef.CreateChild(x));
            }

            return library;
        }

        private IEnumerable<string> GetUniqueNameParts()
        {
            foreach (var parent in this.Parent.GetHierarchy().Reverse())
                yield return parent.Name;

            yield return this.Name;

            yield return this.GroupIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}