using System.Collections.Generic;

namespace Askaiser.Marionette.SourceGenerator;

internal sealed record ParsedName(IReadOnlyList<string> Libraries, string PropertyName, int GroupIndex, decimal Threshold, bool Grayscale);
