using System;
using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// A node of the tree of generated classes: the root is the class marked with the attribute, the other nodes are subdirectories.
/// </summary>
internal sealed class LibraryNode
{
    public LibraryNode(string name, LibraryNode? parent)
    {
        this.Name = name;
        this.Parent = parent;
    }

    /// <summary>Gets the name of the property that exposes this library in its parent.</summary>
    public string Name { get; }

    public LibraryNode? Parent { get; }

    public SortedDictionary<string, LibraryNode> Children { get; } = new SortedDictionary<string, LibraryNode>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the images of this library by property name. A property that has several images returns an array.</summary>
    public SortedDictionary<string, List<ImageEntry>> Images { get; } = new SortedDictionary<string, List<ImageEntry>>(StringComparer.OrdinalIgnoreCase);

    public bool IsEmpty => this.Images.Count == 0 && this.Children.Values.All(static child => child.IsEmpty);

    /// <summary>Gets the name of the generated class of this library.</summary>
    public string TypeName => this.Name + "Library";

    /// <summary>Gets the names of this library and of its ancestors, the root excluded, from the outermost to this one.</summary>
    public IEnumerable<string> Path => this.Parent == null ? [] : this.Parent.Path.Concat([this.Name]);
}
