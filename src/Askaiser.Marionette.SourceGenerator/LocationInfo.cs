using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// A source location that does not keep a syntax tree alive, unlike <see cref="Microsoft.CodeAnalysis.Location"/>.
/// </summary>
internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan) : IEquatable<LocationInfo>
{
    public static LocationInfo From(SyntaxToken token)
    {
        var location = token.GetLocation();
        return new LocationInfo(location.SourceTree?.FilePath ?? string.Empty, location.SourceSpan, location.GetLineSpan().Span);
    }

    public Location ToLocation()
    {
        return Location.Create(this.FilePath, this.TextSpan, this.LineSpan);
    }
}
