using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Askaiser.UITesting
{
    public sealed class LibraryCodeGenerator
    {
        private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".PNG", ".JPEG", ".JPG", ".BMP"
        };

        private readonly long _maxImageFileSize;
        private readonly string _namespaceName;
        private readonly GeneratedLibrary _rootLibrary;
        private readonly List<string> _warnings;

        private LibraryCodeGenerator(LibraryCodeGeneratorOptions options)
        {
            this._maxImageFileSize = options.MaxImageFileSize;
            this._namespaceName = options.NamespaceName;
            this._rootLibrary = new GeneratedLibrary("root");
            this._warnings = new List<string>();
        }

        public static async Task<CodeGenerationResult> Generate(LibraryCodeGeneratorOptions options)
        {
            options.Validate();
            return await new LibraryCodeGenerator(options).Generate(new DirectoryInfo(options.ImageDirectoryPath)).ConfigureAwait(false);
        }

        public async Task<CodeGenerationResult> Generate(DirectoryInfo directory)
        {
            await this.ProcessImagesInDirectory(directory, this._rootLibrary).ConfigureAwait(false);
            var code = this.GenerateCode();
            return new CodeGenerationResult(code, this._warnings);
        }

        private async Task ProcessImagesInDirectory(DirectoryInfo directory, GeneratedLibrary library)
        {
            var imageFiles = directory.EnumerateFiles("*.*").Where(x => SupportedImageExtensions.Contains(x.Extension));

            await this.ProcessImages(imageFiles, library).ConfigureAwait(false);

            foreach (var subDirectory in directory.EnumerateDirectories())
            {
                var localLibraryRef = library;
                var subLibrary = library.Libraries.GetOrCreate(subDirectory.Name, x => localLibraryRef.CreateChild(x));
                await this.ProcessImagesInDirectory(subDirectory, subLibrary).ConfigureAwait(false);
            }
        }

        private async Task ProcessImages(IEnumerable<FileInfo> imageFiles, GeneratedLibrary library)
        {
            foreach (var image in imageFiles)
                await this.ProcessImage(image, library).ConfigureAwait(false);
        }

        private async Task ProcessImage(FileInfo imageFile, GeneratedLibrary library)
        {
            if (imageFile.Length > this._maxImageFileSize)
            {
                this._warnings.Add($"The file '{imageFile.FullName}' size is greater than the supported maximum {this._maxImageFileSize} bytes.");
                return;
            }

            if (imageFile.Length == 0)
            {
                this._warnings.Add($"The file '{imageFile.FullName}' is empty.");
                return;
            }

            GeneratedImage image;

            try
            {
                image = await Task.Run(() => GeneratedImage.Create(imageFile, library)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._warnings.Add($"An error occurred while processing image '{imageFile.FullName}': {ex}");
                return;
            }

            var imageGroup = image.Parent.Images.GetOrCreate(image.Name, x => new List<GeneratedImage>());

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

            sb.Append("using ").Append(nameof(Askaiser)).Append('.').Append(nameof(UITesting)).AppendLine(";");
            sb.AppendLine();
            sb.Append("namespace ").AppendLine(this._namespaceName);
            sb.AppendLine("{");
            GenerateBaseLibraryCode(sb);
            GenerateLibraryCode(this._rootLibrary, sb);
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void GenerateBaseLibraryCode(StringBuilder sb)
        {
            sb.AppendLine("    public abstract class Library");
            sb.AppendLine("    {");
            sb.AppendLine("        protected Library(ElementCollection elements)");
            sb.AppendLine("        {");
            sb.AppendLine("            this.Elements = elements;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        protected ElementCollection Elements { get; }");
            sb.AppendLine("    }");
        }

        private static void GenerateLibraryCode(GeneratedLibrary library, StringBuilder sb)
        {
            sb.AppendLine();
            sb.Append("    public sealed class ").Append(library.UniqueName).AppendLine("Library : Library");
            sb.AppendLine("    {");

            GenerateLibraryConstructorCode(library, sb);
            GenerateLibraryPropertiesCode(library, sb);
            GenerateLibraryElementCreationCode(library, sb);

            sb.AppendLine("    }");

            foreach (var childLibrary in library.Libraries.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                GenerateLibraryCode(childLibrary, sb);
        }

        private static void GenerateLibraryConstructorCode(GeneratedLibrary library, StringBuilder sb)
        {
            sb.Append("        public ")
                .Append(library.UniqueName)
                .AppendLine(library.Level == 0 ? "Library() : base(new ElementCollection())" : "Library(ElementCollection elements) : base(elements)");

            sb.AppendLine("        {");

            foreach (var childLibrary in library.Libraries.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                sb.Append("            this.").Append(childLibrary.Name).Append(" = new ").Append(childLibrary.UniqueName).AppendLine("Library(this.Elements);");

            if (library.Level == 0)
            {
                if (library.Libraries.Count > 0)
                    sb.AppendLine();

                sb.AppendLine("            this.CreateElements();");
            }

            sb.AppendLine("        }");
        }

        private static void GenerateLibraryPropertiesCode(GeneratedLibrary library, StringBuilder sb)
        {
            if (library.Libraries.Count > 0)
                sb.AppendLine();

            foreach (var childLibrary in library.Libraries.Values)
                sb.Append("        public ").Append(childLibrary.UniqueName).Append("Library ").Append(childLibrary.Name).AppendLine(" { get; }");

            if (library.Images.Count > 0)
                sb.AppendLine();

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

        private static void GenerateLibraryElementCreationCode(GeneratedLibrary library, StringBuilder sb)
        {
            if (library.Level > 0)
                return;

            sb.AppendLine();
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
    }
}