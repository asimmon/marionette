using System;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// Everything the generator needs to know about a class marked with the image library attribute.
/// It is compared by value, including the list of files of the directory: code is only generated again when something actually changed.
/// </summary>
internal sealed record LibraryTarget(
    string ClassName,
    string Namespace,
    string DirectoryPath,
    LocationInfo Location,
    bool SupportsUtf8Literals,
    TargetProblem Problem,
    string ProblemArgument,
    EquatableArray<ImageFile> Files) : IEquatable<LibraryTarget>;
