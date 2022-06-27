using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Askaiser.Marionette.SourceGenerator;

public static class SymbolExtensions
{
    public static string GetFullName(this INamedTypeSymbol symbol)
    {
        var namespaceName = GetNamespace(symbol);
        var genericTypes = symbol.Arity > 0 ? "<" + string.Join(",", symbol.TypeArguments.OfType<INamedTypeSymbol>().Select(GetFullName)) + ">" : string.Empty;
        return namespaceName.Length > 0 ? namespaceName + "." + symbol.Name + genericTypes : symbol.Name + genericTypes;
    }

    public static string GetNamespace(this ISymbol symbol)
    {
        var parts = new Stack<string>();
        var iterator = symbol as INamespaceSymbol ?? symbol.ContainingNamespace;

        while (iterator != null)
        {
            if (!string.IsNullOrEmpty(iterator.Name))
            {
                parts.Push(iterator.Name);
            }

            iterator = iterator.ContainingNamespace;
        }

        return string.Join(".", parts);
    }
}
