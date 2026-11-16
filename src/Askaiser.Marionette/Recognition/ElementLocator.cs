using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    private readonly Func<ScreenFrame, IElement, Rectangle, CancellationToken, Task<IReadOnlyList<Rectangle>>> _recognize;

    public ElementLocator(DriverOptions options, ScreenService screen, IReadOnlyList<IElementRecognizer> recognizers, IFailureScreenshotWriter failureWriter, TimeProvider timeProvider)
    {
        this._options = options;
        this._screen = screen;
        this._recognizers = recognizers;
        this._failureWriter = failureWriter;
        this._timeProvider = timeProvider;
        this._recognize = this.RecognizeAsync;
    }

    /// <summary>
    /// Waits for an element to be visible.
    /// </summary>
    public async Task<SearchResult> WaitForAsync(IElement element, TimeSpan waitFor, Rectangle? searchRect, int monitorIndex, NoSingleResultBehavior behavior, CancellationToken cancellationToken)
    {
        var search = await this.SearchAsync([element], requiredCount: 1, waitFor, searchRect, monitorIndex, cancellationToken).ConfigureAwait(false);
        return await this.CompleteAsync(search, search.Results[0], waitFor, behavior).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any of the elements to be visible and returns the first one that is, in the order they were given.
    /// </summary>
    public async Task<SearchResult> WaitForAnyAsync(IReadOnlyList<IElement> elements, TimeSpan waitFor, Rectangle? searchRect, int monitorIndex, NoSingleResultBehavior behavior, CancellationToken cancellationToken)
    {
        var search = await this.SearchAsync(elements, requiredCount: 1, waitFor, searchRect, monitorIndex, cancellationToken).ConfigureAwait(false);
        var result = search.Results.FirstOrDefault(static candidate => candidate.Success) ?? search.Results[0];
        return await this.CompleteAsync(search, result, waitFor, behavior).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for all the elements to be visible. An element that was seen once is considered found, even if it disappears while waiting for the others.
    /// </summary>
    public async Task<SearchResultCollection> WaitForAllAsync(IReadOnlyList<IElement> elements, TimeSpan waitFor, Rectangle? searchRect, int monitorIndex, NoSingleResultBehavior behavior, CancellationToken cancellationToken)
    {
        var search = await this.SearchAsync(elements, requiredCount: elements.Count, waitFor, searchRect, monitorIndex, cancellationToken).ConfigureAwait(false);

        // Report the first element that is missing or ambiguous, as if elements had been searched one after the other.
        foreach (var result in search.Results)
        {
            _ = await this.CompleteAsync(search, result, waitFor, behavior, releaseFrame: false).ConfigureAwait(false);
        }

        search.Frame.Release();
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

    /// <summary>
    /// Looks for elements until <paramref name="requiredCount"/> of them were found or time runs out.
    /// The frame of the returned search belongs to the caller, which must release it.
    /// </summary>
    private async Task<Search> SearchAsync(IReadOnlyList<IElement> elements, int requiredCount, TimeSpan waitFor, Rectangle? searchRect, int monitorIndex, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(waitFor, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var element in elements)
        {
            _ = this.GetRecognizer(element);
        }

        var monitor = await this._screen.GetMonitorAsync(monitorIndex, cancellationToken).ConfigureAwait(false);
        var region = GetSearchRegion(monitor, searchRect);
        var origin = new Point(monitor.Left + region.Left, monitor.Top + region.Top);

        var locations = new IReadOnlyList<Rectangle>[elements.Count];
        Array.Fill(locations, []);
        var foundCount = 0;

        // Several elements are searched in parallel. These lists are reused by every attempt.
        var pendingTasks = elements.Count > 1 ? new List<Task<IReadOnlyList<Rectangle>>>(elements.Count) : null;
        var pendingIndices = elements.Count > 1 ? new List<int>(elements.Count) : null;

        ScreenFrame? frame = null;
        var startTimestamp = this._timeProvider.GetTimestamp();

        try
        {
            while (true)
            {
                var current = await this._screen.AcquireFrameAsync(monitorIndex, cancellationToken).ConfigureAwait(false);
                frame?.Release();
                frame = current;

                if (pendingTasks == null)
                {
                    locations[0] = await this.LocateAsync(current, elements[0], region, cancellationToken).ConfigureAwait(false);
                    foundCount = locations[0].Count > 0 ? 1 : 0;
                }
                else
                {
                    pendingTasks.Clear();
                    pendingIndices!.Clear();

                    // Elements that were already found are not searched again.
                    for (var i = 0; i < elements.Count; i++)
                    {
                        if (locations[i].Count == 0)
                        {
                            pendingIndices.Add(i);
                            pendingTasks.Add(this.LocateAsync(current, elements[i], region, cancellationToken));
                        }
                    }

                    // This waits for every search, even when one of them fails: they all read the frame, which is released on failure.
                    await Task.WhenAll(CollectionsMarshal.AsSpan(pendingTasks)).ConfigureAwait(false);

                    for (var i = 0; i < pendingTasks.Count; i++)
                    {
                        var found = await pendingTasks[i].ConfigureAwait(false);
                        if (found.Count > 0)
                        {
                            locations[pendingIndices[i]] = found;
                            foundCount++;
                        }
                    }
                }

                if (foundCount >= requiredCount || this._timeProvider.GetElapsedTime(startTimestamp) >= waitFor)
                {
                    break;
                }

                if (this._options.WaitForThrottlingInterval > TimeSpan.Zero)
                {
                    await Task.Delay(this._options.WaitForThrottlingInterval, this._timeProvider, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
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

    /// <summary>
    /// Returns the locations of an element in a frame. They are only searched once per frame, whatever the number of callers.
    /// </summary>
    private Task<IReadOnlyList<Rectangle>> LocateAsync(ScreenFrame frame, IElement element, Rectangle region, CancellationToken cancellationToken)
    {
        var task = frame.GetOrAddResult(element, region, this._recognize, cancellationToken);

        // Polling a screen that does not change goes through here: the answer is already known.
        return task.IsCompletedSuccessfully ? task : this.AwaitSharedSearchAsync(task, frame, element, region, cancellationToken);
    }

    private async Task<IReadOnlyList<Rectangle>> AwaitSharedSearchAsync(Task<IReadOnlyList<Rectangle>> task, ScreenFrame frame, IElement element, Rectangle region, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The search was started by another caller of the driver, which canceled it. Start it again for this caller.
                task = frame.GetOrAddResult(element, region, this._recognize, cancellationToken);
            }
        }
    }

    private Task<IReadOnlyList<Rectangle>> RecognizeAsync(ScreenFrame frame, IElement element, Rectangle region, CancellationToken cancellationToken)
    {
        var recognizer = this.GetRecognizer(element);
        return Task.Run(() => recognizer.Recognize(frame.GetSurface(region), frame.Display, element, cancellationToken), cancellationToken);
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
            search.Frame.Release();
            throw;
        }
        finally
        {
            if (releaseFrame)
            {
                search.Frame.Release();
            }
        }
    }

    /// <summary>
    /// Saves the part of the screen that was searched when an element could not be found.
    /// </summary>
    /// <remarks>
    /// This is not canceled along with the search: the search already failed, and what explains why is worth the few milliseconds it takes to write.
    /// </remarks>
    private async Task SaveFailureScreenshotAsync(Search search, IElement element)
    {
        if (this._options.FailureScreenshotPath == null)
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

    private sealed record Search(SearchResult[] Results, ScreenFrame Frame, Rectangle Region);
}
