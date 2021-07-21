using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator
{
    internal static class DiagnosticsDescriptors
    {
        public static readonly DiagnosticDescriptor UnexpectedException = new DiagnosticDescriptor(
            id: "AMSG001",
            title: "Unexpected exception",
            messageFormat: "An unexpected exception occurred: {0}",
            category: nameof(LibrarySourceGenerator),
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingPartialModifier = new DiagnosticDescriptor(
            id: "AMSG002",
            title: "Partial modifier required",
            messageFormat: Constants.ExpectedAttributeFullName + " requires the class '{0}' to use the partial modifier.",
            category: nameof(LibrarySourceGenerator),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidDirectoryPath = new DiagnosticDescriptor(
            id: "AMSG003",
            title: "Invalid image directory path",
            messageFormat: "The image directory path '{0}' is not a non-empty relative or absolute path.",
            category: nameof(LibrarySourceGenerator),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FileTooLarge = new DiagnosticDescriptor(
            id: "AMSG004",
            title: "Image file too large",
            messageFormat: "The image '{0}' size is greater than the supported maximum {1} bytes.",
            category: nameof(LibrarySourceGenerator),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateImageName = new DiagnosticDescriptor(
            id: "AMSG005",
            title: "Duplicate image name",
            messageFormat: "An image named '{0}' already exists, therefore the file '{1}' will be skipped.",
            category: nameof(LibrarySourceGenerator),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NestedClassNotAllowed = new DiagnosticDescriptor(
            id: "AMSG006",
            title: "Nested class not allowed",
            messageFormat: Constants.ExpectedAttributeFullName + " cannot be used on a nested class.",
            category: nameof(LibrarySourceGenerator),
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
