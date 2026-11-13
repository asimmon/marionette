using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator;

#pragma warning disable RS2008 // Enable analyzer release tracking: diagnostics are documented in the README
internal static class DiagnosticDescriptors
{
    private const string Category = "Askaiser.Marionette";

    public static readonly DiagnosticDescriptor UnexpectedException = new DiagnosticDescriptor(
        id: "AMSG001",
        title: "Unexpected exception",
        messageFormat: "An unexpected exception occurred while generating the image library: {0}",
        category: Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingPartialModifier = new DiagnosticDescriptor(
        id: "AMSG002",
        title: "Partial modifier required",
        messageFormat: "The class '{0}' must be partial to use " + Constants.AttributeFullName,
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidDirectoryPath = new DiagnosticDescriptor(
        id: "AMSG003",
        title: "Invalid image directory path",
        messageFormat: "The image directory path '{0}' is not a non-empty relative or absolute path",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FileTooLarge = new DiagnosticDescriptor(
        id: "AMSG004",
        title: "Image file too large",
        messageFormat: "The image '{0}' is larger than the supported maximum of {1} bytes and is ignored",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateImageName = new DiagnosticDescriptor(
        id: "AMSG005",
        title: "Duplicate image name",
        messageFormat: "An image named '{0}' already exists, therefore the file '{1}' is ignored",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedClassNotAllowed = new DiagnosticDescriptor(
        id: "AMSG006",
        title: "Nested or generic class not allowed",
        messageFormat: Constants.AttributeFullName + " cannot be used on the class '{0}' because it is nested or generic",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticClassNotAllowed = new DiagnosticDescriptor(
        id: "AMSG007",
        title: "Static modifier not allowed",
        messageFormat: Constants.AttributeFullName + " cannot be used on the static class '{0}'",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectoryNotFound = new DiagnosticDescriptor(
        id: "AMSG008",
        title: "Image directory not found",
        messageFormat: "The image directory '{0}' does not exist. Relative paths are resolved from the directory of the file that declares the class.",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidImageName = new DiagnosticDescriptor(
        id: "AMSG009",
        title: "Unusable image name",
        messageFormat: "The file '{0}' is ignored because the property '{1}' it would create {2}",
        category: Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
