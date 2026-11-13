using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Screen;

namespace Askaiser.Marionette.Recognition;

/// <summary>
/// Looks for elements on a monitor, repeatedly until they show up or time runs out.
/// </summary>
/// <remarks>
/// Every attempt captures the monitor once, whatever the number of elements, and searches all of them in parallel.
/// Attempts on a screen that did not change since the previous one are free, see <see cref="ScreenFrame"/>.
/// </remarks>
internal sealed partial class ElementLocator
{
    private readonly DriverOptions _options;
    private readonly ScreenService _screen;
    private readonly IReadOnlyList<IElementRecognizer> _recognizers;
    private readonly IFailureScreenshotWriter _failureWriter;
    private readonly TimeProvider _timeProvider;

    public ElementLocator(DriverOptions options, ScreenService screen, IReadOnlyList<IElementRecognizer> recognizers, IFailureScreenshotWriter failureWriter, TimeProvider timeProvider)
    {
        this._options = options;
        this._screen = screen;
        this._recognizers = recognizers;
        this._failureWriter = failureWriter;
        this._timeProvider = timeProvider;
    }

    /// <summary>
    /// Waits for an element to be visible.
    /// </summary>
    public async Task<SearchResult> WaitForAsync(IElement element, TimeSpan waitFor, Rectangle? searchRect, int monitorIndex, NoSingleResultBehavior behavior)
    {
        var search = await this.SearchAsync([element], waitFor, searchRect, monitorIndex, static found => found.Count == 1).ConfigureAwait(false);
        return await this.CompleteAsync(search, search.Results[0], waitFor, behavior).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any of the elements to be visible and returns the first one that is, in the order they were given.
    /// </summary>
    public async Task<SearchResult> WaitForAnyAsync(IReadOnlyList<IElement> elements, TimeSpan waitFor, Rectangle? searchRect, int monitorIndex, NoSingleResultBehavior behavior)
    {
        var search = await this.SearchAsync(elements, waitFor, searchRect, monitorIndex, static found => found.Count > 0).ConfigureAwait(false);
        var result = search.Results.FirstOrDefault(static candidate => candidate.Success) ?? search.Results[0];
        return await this.CompleteAsync(search, result, waitFor, behavior).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for all the elements to be visible. An element that was seen once is considered found, even if it disappears while waiting for the others.
    /// </summary>
    public async Task<SearchResultCollection> WaitForAllAsync(IReadOnlyList<IElement> elements, TimeSpan waitFor, Rectangle? searchRect, int monitorIndex, NoSingleResultBehavior behavior)
    {
        var search = await this.SearchAsync(elements, waitFor, searchRect, monitorIndex, found => found.Count == elements.Count).ConfigureAwait(false);

        // Report the first element that is missing or ambiguous, as if elements had been searched one after the other.
        foreach (var result in search.Results)
        {
            _ = await this.CompleteAsync(search, result, waitFor, behavior, releaseFrame: false).ConfigureAwait(false);
        }

        search.Frame?.Release();
        return new SearchResultCollection(search.Results);
    }

    private static string MakeFailureScreenshotFileName(IElement element, DateTimeOffset now)
    {
        var descriptor = NotAlphanumericRegex().Replace(WhitespaceRegex().Replace(element.ToString()?.Trim() ?? string.Empty, "-"), string.Empty);
        return Messages.FailureScreenshotFileNameFormat.FormatInvariant(now.UtcDateTime, descriptor.ToLowerInvariant());
    }

    [GeneratedRegex("[^a-z0-9\\-]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NotAlphanumericRegex();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex WhitespaceRegex();

    private async Task<Search> SearchAsync(IReadOnlyList<IElement> elements, TimeSpan waitFor, Rectangle? searchRect, int monitorIndex, Func<IReadOnlyCollection<IElement>, bool> isComplete)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(waitFor, TimeSpan.Zero);

        foreach (var element in elements)
        {
            _ = this.GetRecognizer(element);
        }

        var monitor = await this._screen.GetMonitorAsync(monitorIndex).ConfigureAwait(false);
        var region = GetSearchRegion(monitor, searchRect);
        var origin = new Point(monitor.Left + region.Left, monitor.Top + region.Top);

        var locations = new IReadOnlyList<Rectangle>[elements.Count];
        Array.Fill(locations, []);
        var found = new HashSet<IElement>(ReferenceEqualityComparer.Instance);

        ScreenFrame? frame = null;
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            while (true)
            {
                var current = await this._screen.AcquireFrameAsync(monitorIndex, CancellationToken.None).ConfigureAwait(false);
                frame?.Release();
                frame = current;

                // Elements that were already found are not searched again.
                var pending = new List<(int Index, Task<IReadOnlyList<Rectangle>> Task)>();
                for (var i = 0; i < elements.Count; i++)
                {
                    if (!found.Contains(elements[i]))
                    {
                        pending.Add((i, current.GetOrAddResult(elements[i], region, (element, searchRegion) => this.RecognizeAsync(current, element, searchRegion))));
                    }
                }

                await Task.WhenAll(pending.Select(static search => search.Task)).ConfigureAwait(false);

                foreach (var (index, task) in pending)
                {
                    locations[index] = await task.ConfigureAwait(false);
                    if (locations[index].Count > 0)
                    {
                        found.Add(elements[index]);
                    }
                }

                if (isComplete(found) || Stopwatch.GetElapsedTime(startTimestamp) >= waitFor)
                {
                    break;
                }

                if (this._options.WaitForThrottlingInterval > TimeSpan.Zero)
                {
                    await Task.Delay(this._options.WaitForThrottlingInterval, this._timeProvider).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            frame?.Release();
            throw;
        }

        var results = new SearchResult[elements.Count];
        for (var i = 0; i < results.Length; i++)
        {
            // Locations are relative to the searched region: move them to the coordinates of the virtual desktop.
            results[i] = new SearchResult(elements[i], locations[i].Select(location => location + origin));
        }

        return new Search(results, frame, region);
    }

    private static Rectangle GetSearchRegion(MonitorDescription monitor, Rectangle? searchRect)
    {
        var monitorBounds = new Rectangle(0, 0, monitor.Width, monitor.Height);
        if (searchRect == null)
        {
            return monitorBounds;
        }

        // Search rectangles are expressed in the coordinates of the virtual desktop.
        var relative = searchRect - (monitor.Left, monitor.Top);
        return relative.Intersect(monitorBounds)
            ?? throw new ArgumentOutOfRangeException(nameof(searchRect), Messages.Image_Throw_RegionOutOfBounds.FormatInvariant(searchRect, monitor.Width, monitor.Height));
    }

    private IElementRecognizer GetRecognizer(IElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        foreach (var recognizer in this._recognizers)
        {
            if (recognizer.CanRecognize(element))
            {
                return recognizer;
            }
        }

        throw new NotSupportedException(Messages.MarionetteDriver_Throw_UnsupportedElement.FormatInvariant(element.Name, element.GetType().Name));
    }

    private Task<IReadOnlyList<Rectangle>> RecognizeAsync(ScreenFrame frame, IElement element, Rectangle region)
    {
        var recognizer = this.GetRecognizer(element);
        return Task.Run(() => recognizer.Recognize(frame.GetSurface(region), frame.Display, element, CancellationToken.None));
    }

    /// <summary>
    /// Applies the behavior requested when an element was not found exactly once, then releases the frame of the search.
    /// </summary>
    private async Task<SearchResult> CompleteAsync(Search search, SearchResult result, TimeSpan waitFor, NoSingleResultBehavior behavior, bool releaseFrame = true)
    {
        try
        {
            if (behavior == NoSingleResultBehavior.Throw)
            {
                try
                {
                    result.EnsureSingleLocation(waitFor);
                }
                catch (MarionetteException)
                {
                    await this.SaveFailureScreenshotAsync(search, result.Element).ConfigureAwait(false);
                    throw;
                }
            }

            return result;
        }
        catch when (!releaseFrame)
        {
            // The caller keeps the frame for its other results only when everything goes well.
            search.Frame?.Release();
            throw;
        }
        finally
        {
            if (releaseFrame)
            {
                search.Frame?.Release();
            }
        }
    }

    private async Task SaveFailureScreenshotAsync(Search search, IElement element)
    {
        if (this._options.FailureScreenshotPath == null || search.Frame == null)
        {
            return;
        }

        var region = search.Region;
        byte[] png;
        using (var cropped = search.Frame.Pixels.Crop(region.Left, region.Top, region.Width, region.Height))
        {
            png = ImageCodec.EncodePng(cropped.Span, cropped.Width, cropped.Height);
        }

        var fileName = MakeFailureScreenshotFileName(element, this._timeProvider.GetUtcNow());
        await this._failureWriter.SaveAsync(Path.Combine(this._options.FailureScreenshotPath, fileName), png).ConfigureAwait(false);
    }

    private sealed record Search(SearchResult[] Results, ScreenFrame? Frame, Rectangle Region);
}
