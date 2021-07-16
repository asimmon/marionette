using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Askaiser.Marionette.SourceGenerator
{
    public class LibraryCodeGenerator
    {
        private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".PNG", ".JPEG", ".JPG", ".BMP"
        };

        private readonly long _maxImageFileSize;
        private readonly string _namespaceName;
        private readonly string _className;
        private readonly DirectoryInfo _imageDirectory;
        private readonly GeneratedLibrary _rootLibrary;
        private readonly List<string> _warnings;

        private LibraryCodeGenerator(LibraryCodeGeneratorOptions options)
        {
            this._maxImageFileSize = options.MaxImageFileSize;
            this._namespaceName = options.NamespaceName;
            this._className = options.ClassName;
            this._imageDirectory = new DirectoryInfo(options.ImageDirectoryPath);
            this._rootLibrary = new GeneratedLibrary("root");
            this._warnings = new List<string>();
        }

        public static CodeGenerationResult Generate(LibraryCodeGeneratorOptions options)
        {
            options.Validate();
            return new LibraryCodeGenerator(options).Generate();
        }

        private CodeGenerationResult Generate()
        {
            this.ProcessImagesInDirectory(this._imageDirectory, this._rootLibrary);
            var code = this.GenerateCode();
            return new CodeGenerationResult(code, this._warnings);
        }

        private void ProcessImagesInDirectory(DirectoryInfo directory, GeneratedLibrary library)
        {
            var imageFiles = directory.EnumerateFiles("*.*").Where(x => SupportedImageExtensions.Contains(x.Extension));

            this.ProcessImages(imageFiles, library);

            foreach (var subDirectory in directory.EnumerateDirectories())
            {
                var localLibraryRef = library;
                var subLibrary = library.Libraries.GetOrCreate(subDirectory.Name, x => localLibraryRef.CreateChild(x));
                this.ProcessImagesInDirectory(subDirectory, subLibrary);
            }
        }

        private void ProcessImages(IEnumerable<FileInfo> imageFiles, GeneratedLibrary library)
        {
            foreach (var image in imageFiles)
                this.ProcessImage(image, library);
        }

        private void ProcessImage(FileInfo imageFile, GeneratedLibrary library)
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
                image = GeneratedImage.Create(imageFile, library);
            }
            catch (Exception ex)
            {
                this._warnings.Add($"An error occurred while processing image '{imageFile.FullName}': {ex}");
                return;
            }

            var imageGroup = image.Parent.Images.GetOrCreate(image.Name, _ => new List<GeneratedImage>());

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
            var cw = new CodeWriter();

            cw.Append("// Code generated from the following directory at ").AppendLine(DateTime.UtcNow.ToString("O"));
            cw.Append("// ").AppendLine(this._imageDirectory.FullName);
            cw.Append("using ").Append(nameof(Askaiser)).Append(".").Append(nameof(Marionette)).AppendLine(";");
            cw.AppendLine();

            IDisposable ns = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(this._namespaceName))
                    ns = cw.BeginNamespace(this._namespaceName);

                GenerateBaseLibraryCode(cw);
                this.GenerateLibraryCode(this._rootLibrary, cw);
            }
            finally
            {
                ns?.Dispose();
            }

            return cw.ToString();
        }

        private static void GenerateBaseLibraryCode(CodeWriter cw)
        {
            using (cw.BeginClass("public abstract", "BaseLibrary"))
            {
                cw.AppendLine("protected ElementCollection _elements;");
                cw.AppendLine();
                cw.AppendLine("protected BaseLibrary(ElementCollection elements)");
                using (cw.BeginBlock())
                    cw.AppendLine("this._elements = elements;");
            }
        }

        private void GenerateLibraryCode(GeneratedLibrary library, CodeWriter cw)
        {
            var className = library.IsRoot ? this._className : library.UniqueName + "Library";

            cw.AppendLine();
            using (cw.BeginClass("public partial", className, "BaseLibrary"))
            {
                this.GenerateLibraryConstructorCode(library, cw);
                GenerateLibraryPropertiesCode(library, cw);
                GenerateLibraryElementCreationCode(library, cw);
            }

            foreach (var childLibrary in library.Libraries.Values.Where(x => !x.IsEmpty).OrderBy(x => x.UniqueName, StringComparer.OrdinalIgnoreCase))
                this.GenerateLibraryCode(childLibrary, cw);
        }

        private void GenerateLibraryConstructorCode(GeneratedLibrary library, CodeWriter cw)
        {
            var className = library.IsRoot ? this._className : library.UniqueName + "Library";

            cw.Append("public ")
                .Append(className)
                .AppendLine(library.IsRoot ? "() : base(new ElementCollection())" : "(ElementCollection elements) : base(elements)");

            using (cw.BeginBlock())
            {
                foreach (var childLibrary in library.Libraries.Values.Where(x => !x.IsEmpty).OrderBy(x => x.UniqueName, StringComparer.OrdinalIgnoreCase))
                    cw.Append("this.").Append(childLibrary.Name).Append(" = new ").Append(childLibrary.UniqueName).AppendLine("Library(this._elements);");

                if (library.IsRoot)
                {
                    if (library.Libraries.Count > 0)
                        cw.AppendLine();

                    cw.AppendLine("this.CreateElements();");
                }
            }
        }

        private static void GenerateLibraryPropertiesCode(GeneratedLibrary library, CodeWriter cw)
        {
            if (library.Libraries.Count > 0)
                cw.AppendLine();

            foreach (var childLibrary in library.Libraries.Values.Where(x => !x.IsEmpty).OrderBy(x => x.UniqueName, StringComparer.OrdinalIgnoreCase))
                cw.Append("public ").Append(childLibrary.UniqueName).Append("Library ").Append(childLibrary.Name).AppendLine(" { get; }");

            if (library.Images.Count > 0)
                cw.AppendLine();

            foreach (var imageGroup in library.Images.Values)
            {
                if (imageGroup.Count == 1)
                {
                    cw.Append("public IElement ").Append(imageGroup[0].Name).Append(" => this._elements[\"").Append(imageGroup[0].UniqueName).AppendLine("\"];");
                }
                else
                {
                    cw.Append("public IElement[] ").Append(imageGroup[0].Name).AppendLine(" => new[]");
                    using (cw.BeginBlock(";"))
                    {
                        for (var i = 0; i < imageGroup.Count; i++)
                            cw.Append("this._elements[\"").Append(imageGroup[i].UniqueName).AppendLine(i == imageGroup.Count - 1 ? "\"]" : "\"],");
                    }
                }
            }
        }

        private static void GenerateLibraryElementCreationCode(GeneratedLibrary library, CodeWriter cw)
        {
            if (library.Level > 0)
                return;

            cw.AppendLine();
            cw.AppendLine("private void CreateElements()");
            using (cw.BeginBlock())
            {
                foreach (var image in library.GetImagesChildren().OrderBy(x => x.UniqueName, StringComparer.OrdinalIgnoreCase))
                {
                    cw.Append("this._elements.Add(new ImageElement(\"")
                        .Append(image.UniqueName)
                        .Append("\", \"")
                        .Append(Convert.ToBase64String(image.Bytes))
                        .Append("\", ")
                        .Append(image.Threshold.ToString(CultureInfo.InvariantCulture))
                        .Append("m, ").Append(image.Grayscale ? "true" : "false")
                        .AppendLine("));");
                }
            }
        }
    }
}
