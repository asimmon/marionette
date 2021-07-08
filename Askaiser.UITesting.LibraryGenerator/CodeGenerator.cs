using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Askaiser.UITesting.LibraryGenerator
{
    public sealed class CodeGenerator
    {
        private readonly string _namespaceName;
        private const long MaxImageFileSize = 2 * 1024 * 1024;

        private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".PNG", ".JPEG", ".JPG", ".BMP"
        };

        private readonly Library _rootLibrary;
        private readonly List<string> _warnings;

        public CodeGenerator(string namespaceName)
        {
            this._namespaceName = namespaceName;
            this._rootLibrary = new Library("root");
            this._warnings = new List<string>();
        }

        public async Task<CodeResult> ProcessImagesInDirectory(DirectoryInfo directory)
        {
            var imageFiles = directory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                .Where(x => SupportedImageExtensions.Contains(x.Extension))
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var code = await this.ProcessImages(imageFiles);
            return new CodeResult(code, this._warnings);
        }

        private async Task<string> ProcessImages(IEnumerable<FileInfo> imageFiles)
        {
            foreach (var image in imageFiles)
                await this.CrawlImage(image);

            return this.GenerateCode();
        }

        private async Task CrawlImage(FileInfo imageFile)
        {
            if (imageFile.Length > MaxImageFileSize)
            {
                this._warnings.Add($"The file '{imageFile.FullName}' size is greater than 2MB ({imageFile.Length} bytes).");
                return;
            }

            if (imageFile.Length == 0)
            {
                this._warnings.Add($"The file '{imageFile.FullName}' is empty.");
                return;
            }

            Image image;

            try
            {
                image = await Image.Create(imageFile, this._rootLibrary);
            }
            catch (Exception ex)
            {
                this._warnings.Add($"An error occurred while processing image '{imageFile.FullName}': {ex}");
                return;
            }

            var imageGroup = image.Parent.Images.GetOrCreate(image.Name, x => new List<Image>());

            if (imageGroup.Any(x => x.GroupIndex == image.GroupIndex))
            {
                this._warnings.Add($"An image named '{image.UniqueName}' already exists, therefore the image {imageFile.FullName} will be skipped.");
            }
            else
            {
                imageGroup.Add(image);
            }
        }

        private string GenerateCode()
        {
            var sb = new StringBuilder();

            sb.AppendLine("using Askaiser.UITesting;");
            sb.AppendLine("");
            sb.Append("namespace ").AppendLine(this._namespaceName);
            sb.AppendLine("{");
            sb.AppendLine("    public abstract class Library");
            sb.AppendLine("    {");
            sb.AppendLine("        protected Library(ElementCollection elements)");
            sb.AppendLine("        {");
            sb.AppendLine("            this.Elements = elements;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        protected ElementCollection Elements { get; }");
            sb.AppendLine("    }");

            GenerateLibraryCode(this._rootLibrary, sb);

            // very end
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void GenerateLibraryCode(Library library, StringBuilder sb)
        {
            sb.AppendLine("");
            sb.Append("    public sealed class ").Append(library.UniqueName).AppendLine("Library : Library");
            sb.AppendLine("    {");

            GenerateLibraryConstructorCode(library, sb);
            GenerateLibraryPropertiesCode(library, sb);

            if (library.Level == 0)
                GenerateRootLibraryElementCreationCode(library, sb);

            sb.AppendLine("    }");

            foreach (var childLibrary in library.Libraries.Values)
                GenerateLibraryCode(childLibrary, sb);
        }

        private static void GenerateRootLibraryElementCreationCode(Library library, StringBuilder sb)
        {
            sb.AppendLine("");
            sb.AppendLine("        private void CreateElements()");
            sb.AppendLine("        {");

            foreach (var image in library.GetImagesChildren())
            {
                sb.Append("            this.Elements.Add(new ImageElement(\"")
                    .Append(image.UniqueName)
                    .Append("\", \"")
                    .Append(Convert.ToBase64String(image.Bytes))
                    .Append("\", ")
                    .Append(image.Threshold.ToString(CultureInfo.InvariantCulture))
                    .Append("m, ").Append(image.Grayscale ? "true" : "false")
                    .AppendLine("));");
            }

            sb.AppendLine("        }");
        }

        private static void GenerateLibraryPropertiesCode(Library library, StringBuilder sb)
        {
            if (library.Libraries.Count > 0)
                sb.AppendLine("");

            foreach (var childLibrary in library.Libraries.Values)
                sb.Append("        public ").Append(childLibrary.UniqueName).Append("Library ").Append(childLibrary.Name).AppendLine(" { get; }");

            if (library.Images.Count > 0)
                sb.AppendLine("");

            foreach (var imageGroup in library.Images.Values)
            {
                if (imageGroup.Count == 1)
                {
                    sb.Append("        public IElement ").Append(imageGroup[0].Name).Append(" => this.Elements[\"").Append(imageGroup[0].UniqueName).AppendLine("\"];");
                }
                else
                {
                    sb.Append("        public IElement[] ").Append(imageGroup[0].Name).AppendLine(" => new[]");
                    sb.AppendLine("        {");

                    for (var i = 0; i < imageGroup.Count; i++)
                        sb.Append("            this.Elements[\"").Append(imageGroup[i].UniqueName).AppendLine(i == imageGroup.Count - 1 ? "\"]" : "\"],");

                    sb.AppendLine("        };");
                }
            }
        }

        private static void GenerateLibraryConstructorCode(Library library, StringBuilder sb)
        {
            sb.Append("        public ")
                .Append(library.UniqueName)
                .AppendLine(library.Level == 0 ? "Library() : base(new ElementCollection())" : "Library(ElementCollection elements) : base(elements)");

            sb.AppendLine("        {");

            foreach (var childLibrary in library.Libraries.Values)
                sb.Append("            this.").Append(childLibrary.Name).Append(" = new ").Append(childLibrary.UniqueName).AppendLine("Library(this.Elements);");

            if (library.Level == 0)
            {
                if (library.Libraries.Count > 0)
                    sb.AppendLine("");

                sb.AppendLine("            this.CreateElements();");
            }

            sb.AppendLine("        }");
        }
    }
}