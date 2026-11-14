using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Recognition;
using Askaiser.Marionette.Recognition.Images;

namespace Askaiser.Marionette.Tests.Fakes;

/// <summary>
/// Returns locations decided by the test, relative to the searched region like a real recognizer does.
/// </summary>
internal sealed class FakeElementRecognizer : IElementRecognizer
{
    private readonly Dictionary<IElement, Func<int, CancellationToken, IReadOnlyList<Rectangle>>> _results = [];
    private readonly Dictionary<IElement, int> _callCounts = [];
    private readonly Lock _gate = new();

    public int CallCount
    {
        get
        {
            lock (this._gate)
            {
                return this._callCounts.Values.Sum();
            }
        }
    }

    /// <summary>Gets the size of the last searched surface.</summary>
    public (int Width, int Height) LastSurfaceSize { get; private set; }

    public void Setup(IElement element, params Rectangle[] locations)
    {
        this._results[element] = (_, _) => locations;
    }

    /// <summary>
    /// Sets up locations that depend on the number of times the element was searched, starting at 1.
    /// </summary>
    public void Setup(IElement element, Func<int, IReadOnlyList<Rectangle>> locations)
    {
        this._results[element] = (attempt, _) => locations(attempt);
    }

    /// <summary>
    /// Sets up a search that depends on the number of times the element was searched, starting at 1, and that can observe its cancellation.
    /// </summary>
    public void Setup(IElement element, Func<int, CancellationToken, IReadOnlyList<Rectangle>> search)
    {
        this._results[element] = search;
    }

    public int GetCallCount(IElement element)
    {
        lock (this._gate)
        {
            return this._callCounts.GetValueOrDefault(element);
        }
    }

    public bool CanRecognize(IElement element) => element is FakeElement;

    public IReadOnlyList<Rectangle> Recognize(SearchSurface surface, DisplayInfo display, IElement element, CancellationToken cancellationToken)
    {
        int count;
        lock (this._gate)
        {
            count = this._callCounts[element] = this._callCounts.GetValueOrDefault(element) + 1;
            this.LastSurfaceSize = (surface.Width, surface.Height);
        }

        return this._results.TryGetValue(element, out var locations)
            ? locations(count, cancellationToken)
            : throw new InvalidOperationException($"No result was set up for the element '{element}'.");
    }
}
