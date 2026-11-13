using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Platform;
using Askaiser.Marionette.Recognition.Images;

namespace Askaiser.Marionette.Screen;

/// <summary>
/// What a monitor displayed at a point in time, along with everything that was computed from it.
/// </summary>
/// <remarks>
/// A frame is shared by the searches that run on it, possibly at the same time, and is kept by the driver for as long as the
/// screen does not change. It is therefore reference counted: its memory goes back to the pool when its last user releases it.
/// </remarks>
internal sealed class ScreenFrame
{
    private readonly PixelBuffer _pixels;
    private readonly ConcurrentDictionary<Rectangle, Lazy<SearchSurface>> _surfaces = new();
    private readonly ConcurrentDictionary<(IElement Element, Rectangle Region), Lazy<Task<IReadOnlyList<Rectangle>>>> _results = new();
    private int _references = 1;

    public ScreenFrame(PixelBuffer pixels, DisplayInfo display)
    {
        this._pixels = pixels;
        this.Display = display;
    }

    public DisplayInfo Display { get; }

    public PixelBuffer Pixels => this._pixels;

    /// <summary>Gets the bounds of the frame in its own coordinates.</summary>
    public Rectangle Bounds => new Rectangle(0, 0, this._pixels.Width, this._pixels.Height);

    /// <summary>
    /// Takes a reference on the frame. Returns false when the frame was already released by all its users.
    /// </summary>
    public bool TryAddReference()
    {
        while (true)
        {
            var current = Volatile.Read(ref this._references);
            if (current <= 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref this._references, current + 1, current) == current)
            {
                return true;
            }
        }
    }

    public void Release()
    {
        if (Interlocked.Decrement(ref this._references) != 0)
        {
            return;
        }

        foreach (var surface in this._surfaces.Values)
        {
            if (surface.IsValueCreated)
            {
                surface.Value.Dispose();
            }
        }

        this._pixels.Dispose();
    }

    /// <summary>
    /// Gets the part of the frame delimited by a region, in frame coordinates, in a form that can be searched.
    /// </summary>
    public SearchSurface GetSurface(Rectangle region)
    {
        return this._surfaces.GetOrAdd(region, static (key, self) => new Lazy<SearchSurface>(() => self.CreateSurface(key), LazyThreadSafetyMode.ExecutionAndPublication), this).Value;
    }

    /// <summary>
    /// Gets the locations of an element in a region of this frame, searching for it only the first time.
    /// Polling a screen that does not change therefore costs nothing more than capturing it.
    /// </summary>
    public Task<IReadOnlyList<Rectangle>> GetOrAddResult(IElement element, Rectangle region, Func<IElement, Rectangle, Task<IReadOnlyList<Rectangle>>> search)
    {
        var lazy = this._results.GetOrAdd((element, region), static (key, search) => new Lazy<Task<IReadOnlyList<Rectangle>>>(() => search(key.Element, key.Region), LazyThreadSafetyMode.ExecutionAndPublication), search);
        var task = lazy.Value;

        if (task.IsFaulted || task.IsCanceled)
        {
            // Do not remember failures: the next attempt may succeed.
            this._results.TryRemove(new KeyValuePair<(IElement, Rectangle), Lazy<Task<IReadOnlyList<Rectangle>>>>((element, region), lazy));
        }

        return task;
    }

    private SearchSurface CreateSurface(Rectangle region)
    {
        return region == this.Bounds
            ? new SearchSurface(this._pixels, ownsPixels: false)
            : new SearchSurface(this._pixels.Crop(region.Left, region.Top, region.Width, region.Height), ownsPixels: true);
    }
}
