using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// Builds the tree of libraries and images of a class from the files of its directory.
/// </summary>
internal sealed class LibraryBuilder
{
    private const string InstancePropertyName = "Instance";

    private readonly LibraryTarget _target;
    private readonly IFileSystem _fileSystem;
    private readonly Location _location;
    private readonly List<Diagnostic> _diagnostics = [];

    public LibraryBuilder(LibraryTarget target, IFileSystem fileSystem)
    {
        this._target = target;
        this._fileSystem = fileSystem;
        this._location = target.Location.ToLocation();
    }

    public IReadOnlyList<Diagnostic> Diagnostics => this._diagnostics;

    public LibraryNode Build()
    {
        var root = new LibraryNode(this._target.ClassName, parent: null);

        // Sorting makes the generated code independent of the order the file system lists files in.
        foreach (var file in this._target.Files.Where(static file => LibraryNaming.IsImage(file.RelativePath)).OrderBy(static file => file.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            this.AddImage(root, file);
        }

        return root;
    }

    private void AddImage(LibraryNode root, ImageFile file)
    {
        if (file.Length == 0)
        {
            return;
        }

        if (file.Length > Constants.MaxImageSize)
        {
            this.Report(DiagnosticDescriptors.FileTooLarge, file.RelativePath, Constants.MaxImageSize.ToString(CultureInfo.InvariantCulture));
            return;
        }

        var parsed = LibraryNaming.Parse(file.RelativePath);
        if (parsed == null)
        {
            this.Report(DiagnosticDescriptors.InvalidImageName, file.RelativePath, string.Empty, "would have an empty name");
            return;
        }

        // Find the library of the image, checking that every level can be exposed as a property of its parent.
        var library = root;
        var created = new List<LibraryNode>();

        foreach (var libraryName in parsed.Libraries)
        {
            if (!library.Children.TryGetValue(libraryName, out var child))
            {
                if (this.GetNameProblem(library, libraryName, isLibrary: true) is { } libraryProblem)
                {
                    this.Report(DiagnosticDescriptors.InvalidImageName, file.RelativePath, libraryName, libraryProblem);
                    RemoveCreatedLibraries(created);
                    return;
                }

                child = new LibraryNode(libraryName, library);
                library.Children.Add(libraryName, child);
                created.Add(child);
            }

            library = child;
        }

        if (this.GetNameProblem(library, parsed.PropertyName, isLibrary: false) is { } problem)
        {
            this.Report(DiagnosticDescriptors.InvalidImageName, file.RelativePath, parsed.PropertyName, problem);
            RemoveCreatedLibraries(created);
            return;
        }

        var image = new ImageEntry(file, library, parsed.PropertyName, parsed.GroupIndex, parsed.Threshold, parsed.Grayscale);

        if (!library.Images.TryGetValue(image.Name, out var group))
        {
            library.Images.Add(image.Name, group = []);
        }

        if (group.Exists(other => other.GroupIndex == image.GroupIndex))
        {
            this.Report(DiagnosticDescriptors.DuplicateImageName, image.ElementName, file.RelativePath);
            RemoveCreatedLibraries(created);
            return;
        }

        image.Content = this._fileSystem.ReadAllBytes(file.FullPath);
        if (image.Content.Length == 0)
        {
            if (group.Count == 0)
            {
                library.Images.Remove(image.Name);
            }

            RemoveCreatedLibraries(created);
            return;
        }

        group.Add(image);
        group.Sort(static (a, b) => a.GroupIndex.CompareTo(b.GroupIndex));
    }

    private static void RemoveCreatedLibraries(List<LibraryNode> created)
    {
        // Libraries that were only created for an image that ends up ignored must not be generated.
        for (var i = created.Count - 1; i >= 0; i--)
        {
            created[i].Parent!.Children.Remove(created[i].Name);
        }
    }

    /// <summary>
    /// Explains why a name cannot be used for a property of a library, or returns null when it can.
    /// </summary>
    private string? GetNameProblem(LibraryNode library, string name, bool isLibrary)
    {
        var enclosingTypeName = library.Parent == null ? this._target.ClassName : library.TypeName;

        if (string.Equals(name, enclosingTypeName, StringComparison.Ordinal))
        {
            return "would have the same name as its class";
        }

        if (isLibrary && string.Equals(name + "Library", enclosingTypeName, StringComparison.Ordinal))
        {
            return "would need a class with the same name as the class that contains it";
        }

        if (library.Parent == null && string.Equals(name, InstancePropertyName, StringComparison.Ordinal))
        {
            return "conflicts with the generated 'Instance' property";
        }

        if (isLibrary && library.Images.ContainsKey(name))
        {
            return "conflicts with an image of the same name";
        }

        if (isLibrary && library.Images.ContainsKey(name + "Library"))
        {
            return "needs a class whose name conflicts with an image";
        }

        if (!isLibrary && library.Children.ContainsKey(name))
        {
            return "conflicts with a directory of the same name";
        }

        if (!isLibrary && library.Children.Values.Any(child => string.Equals(child.TypeName, name, StringComparison.OrdinalIgnoreCase)))
        {
            return "conflicts with the class generated for a directory";
        }

        return null;
    }

    private void Report(DiagnosticDescriptor descriptor, params object[] arguments)
    {
        this._diagnostics.Add(Diagnostic.Create(descriptor, this._location, arguments));
    }
}
