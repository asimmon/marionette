using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[assembly: InternalsVisibleTo("Askaiser.Marionette.SourceGenerator.Tests")]

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// Generates, for every partial class marked with <c>[ImageLibrary("directory")]</c>, properties that expose the images of the directory as elements.
/// </summary>
/// <remarks>
/// The generator is incremental. While code is being edited, it only looks at the names, sizes and modification times of the
/// files of a library, at most once per second. Images are read and code is generated again only when one of those changed.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ImageLibraryGenerator : IIncrementalGenerator
{
    private readonly IFileSystem _fileSystem;
    private readonly TimeSpan _directoryListingLifetime;
    private readonly ConcurrentDictionary<string, DirectoryListing> _directoryListings = new ConcurrentDictionary<string, DirectoryListing>(StringComparer.Ordinal);

    public ImageLibraryGenerator()
        : this(new PhysicalFileSystem(), TimeSpan.FromSeconds(1))
    {
    }

    internal ImageLibraryGenerator(IFileSystem fileSystem, TimeSpan directoryListingLifetime)
    {
        this._fileSystem = fileSystem;
        this._directoryListingLifetime = directoryListingLifetime;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            Constants.AttributeFullName,
            static (node, _) => node is ClassDeclarationSyntax,
            this.CreateTarget);

        context.RegisterSourceOutput(targets, this.Generate);
    }

    private static bool TryNormalizePath(string? rawPath, out string path)
    {
        path = rawPath?.Trim() ?? string.Empty;
        if (path.Length == 0)
        {
            return false;
        }

        try
        {
            _ = Path.GetFullPath(path);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private LibraryTarget CreateTarget(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var classSyntax = (ClassDeclarationSyntax)context.TargetNode;
        var classSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var namespaceName = classSymbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace ? containingNamespace.ToDisplayString() : string.Empty;
        var supportsUtf8Literals = classSyntax.SyntaxTree.Options is CSharpParseOptions { LanguageVersion: >= LanguageVersion.CSharp11 };

        var target = new LibraryTarget(classSymbol.Name, namespaceName, string.Empty, LocationInfo.From(classSyntax.Identifier), supportsUtf8Literals, TargetProblem.None, string.Empty, default);

        if (!classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return target with { Problem = TargetProblem.MissingPartialModifier, ProblemArgument = classSymbol.ToDisplayString() };
        }

        if (classSymbol.IsStatic)
        {
            return target with { Problem = TargetProblem.StaticClass, ProblemArgument = classSymbol.ToDisplayString() };
        }

        if (classSymbol.ContainingType != null || classSymbol.IsGenericType)
        {
            return target with { Problem = TargetProblem.NestedOrGenericClass, ProblemArgument = classSymbol.ToDisplayString() };
        }

        var rawPath = context.Attributes.Length > 0 && context.Attributes[0].ConstructorArguments.Length > 0
            ? context.Attributes[0].ConstructorArguments[0].Value as string
            : null;

        if (!TryNormalizePath(rawPath, out var directoryPath))
        {
            return target with { Problem = TargetProblem.InvalidDirectoryPath, ProblemArgument = rawPath ?? string.Empty };
        }

        // Relative paths are relative to the file that declares the class.
        if (!Path.IsPathRooted(directoryPath) && Path.GetDirectoryName(classSyntax.SyntaxTree.FilePath) is { Length: > 0 } sourceDirectory)
        {
            directoryPath = Path.Combine(sourceDirectory, directoryPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var listing = this.ListDirectory(directoryPath);
            return listing.Exists
                ? target with { DirectoryPath = directoryPath, Files = listing.Files }
                : target with { DirectoryPath = directoryPath, Problem = TargetProblem.DirectoryNotFound, ProblemArgument = directoryPath };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return target with { DirectoryPath = directoryPath, Problem = TargetProblem.UnexpectedException, ProblemArgument = ex.Message };
        }
    }

    /// <summary>
    /// Lists the files of a directory. The generator runs on every keystroke in an IDE, so a recent listing is reused.
    /// </summary>
    private DirectoryListing ListDirectory(string directoryPath)
    {
        if (this._directoryListings.TryGetValue(directoryPath, out var cached) && Stopwatch.GetTimestamp() - cached.Timestamp <= this._directoryListingLifetime.TotalSeconds * Stopwatch.Frequency)
        {
            return cached;
        }

        var listing = this._fileSystem.DirectoryExists(directoryPath)
            ? new DirectoryListing(Stopwatch.GetTimestamp(), Exists: true, new EquatableArray<ImageFile>(this._fileSystem.EnumerateFiles(directoryPath).Where(static file => LibraryNaming.IsImage(file.RelativePath)).OrderBy(static file => file.RelativePath, StringComparer.Ordinal)))
            : new DirectoryListing(Stopwatch.GetTimestamp(), Exists: false, default);

        this._directoryListings[directoryPath] = listing;
        return listing;
    }

    private void Generate(SourceProductionContext context, LibraryTarget target)
    {
        var location = target.Location.ToLocation();

        var descriptor = target.Problem switch
        {
            TargetProblem.MissingPartialModifier => DiagnosticDescriptors.MissingPartialModifier,
            TargetProblem.InvalidDirectoryPath => DiagnosticDescriptors.InvalidDirectoryPath,
            TargetProblem.NestedOrGenericClass => DiagnosticDescriptors.NestedClassNotAllowed,
            TargetProblem.StaticClass => DiagnosticDescriptors.StaticClassNotAllowed,
            TargetProblem.DirectoryNotFound => DiagnosticDescriptors.DirectoryNotFound,
            TargetProblem.UnexpectedException => DiagnosticDescriptors.UnexpectedException,
            _ => null,
        };

        if (descriptor != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, target.ProblemArgument));

            // Without a directory there is nothing to expose, but the class still gets its members so that code using it keeps compiling.
            if (target.Problem != TargetProblem.DirectoryNotFound)
            {
                return;
            }
        }

        try
        {
            var builder = new LibraryBuilder(target, this._fileSystem);
            var root = builder.Build();

            foreach (var diagnostic in builder.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            var hintName = (target.Namespace.Length > 0 ? target.Namespace + "." : string.Empty) + target.ClassName + ".images.g.cs";
            context.AddSource(hintName, SourceText.From(new LibraryEmitter(target).Emit(root), Encoding.UTF8));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnexpectedException, location, ex.ToString()));
        }
    }

    private sealed record DirectoryListing(long Timestamp, bool Exists, EquatableArray<ImageFile> Files);
}
