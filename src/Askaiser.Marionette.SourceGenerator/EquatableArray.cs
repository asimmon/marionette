using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// An immutable array compared by its content. The models that flow through an incremental generator must have
/// value equality, otherwise the generator cannot tell that nothing changed and runs again.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    private readonly T[]? _items;

    public EquatableArray(IEnumerable<T> items)
    {
        this._items = items.ToArray();
    }

    public int Count => this._items?.Length ?? 0;

    public T this[int index] => this._items![index];

    public bool Equals(EquatableArray<T> other)
    {
        return this.AsSpan().SequenceEqual(other.AsSpan());
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in this.AsSpan())
        {
            hash = unchecked((hash * 31) + item.GetHashCode());
        }

        return hash;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)(this._items ?? [])).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    private ReadOnlySpan<T> AsSpan()
    {
        return this._items;
    }
}
