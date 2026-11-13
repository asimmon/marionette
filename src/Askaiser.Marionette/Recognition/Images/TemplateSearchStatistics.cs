using System;

namespace Askaiser.Marionette.Recognition.Images;

/// <summary>
/// Counts how many screen positions reach each stage of a search. Only used to tune and test the search.
/// </summary>
internal sealed class TemplateSearchStatistics
{
    private readonly object _gate = new();

    public long Positions { get; private set; }

    /// <summary>Gets the number of positions that passed each coarse level, followed by the number of positions above the threshold.</summary>
    public long[] Passed { get; private set; } = Array.Empty<long>();

    public void Add(long positions, long[] passed)
    {
        lock (this._gate)
        {
            if (this.Passed.Length != passed.Length)
            {
                this.Passed = new long[passed.Length];
            }

            this.Positions += positions;
            for (var i = 0; i < passed.Length; i++)
            {
                this.Passed[i] += passed[i];
            }
        }
    }
}
