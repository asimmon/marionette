using System;
using System.Collections.Generic;

namespace Askaiser.Marionette.Platform.MacOS;

/// <summary>
/// Places the displays of a macOS desktop in the pixel coordinates Marionette works in.
/// </summary>
/// <remarks>
/// <para>
/// macOS positions windows and the cursor in points, while a capture is made of pixels: a display of 1512x982 points
/// whose scale factor is 2 is captured as 3024x1964 pixels. Marionette works in pixels everywhere, so every display
/// has to be given bounds in a single pixel coordinate space, the virtual desktop. A display keeps the size of its own
/// captures there: its size in points multiplied by its own scale factor.
/// </para>
/// <para>
/// Origins cannot be computed the same way. Multiplying the points origin of a display by its own scale factor, which
/// is right when every display has the same scale factor, places a 1920x1080 display at scale 1 that sits at the right
/// of a 1512x982 Retina display 1512 pixels from the origin, in the middle of the 3024 pixels of the Retina display.
/// Origins therefore come from the layout of the whole desktop: each axis is cut at the edges of the displays, each of
/// the resulting intervals is given a length in pixels, and the origin of a display is the sum of the lengths that
/// precede it. The main display is at the origin, and displays on its left or above it have negative coordinates, as
/// on the other platforms.
/// </para>
/// <para>
/// An interval is as long as the number of pixels shown there by the display with the most pixels per point, so a
/// display never begins before the end of the display on its left or above it: pixel bounds never overlap, and
/// coordinates in pixels therefore always designate a single monitor. Both systems are exact inverses of each other
/// on every display, which is what the mouse needs. A desktop whose displays have different scale factors and are not
/// simply in a row or a column may leave unused space between pixel bounds: nothing is displayed there, and nothing is
/// searched there either, as a search always happens in a capture of a single monitor.
/// </para>
/// </remarks>
internal static class MacDisplayLayout
{
    /// <summary>Absorbs the rounding error of scale factors that are not whole numbers, such as those of scaled display modes.</summary>
    private const double PixelTolerance = 1e-6;

    /// <summary>
    /// Returns the displays of the desktop with their bounds in pixels, in the same order as <paramref name="displays"/>.
    /// </summary>
    public static DisplayGeometry[] Compute(IReadOnlyList<MacDisplay> displays)
    {
        if (displays.Count == 0)
        {
            return [];
        }

        var horizontal = Axis.Build(displays, vertical: false);
        var vertical = Axis.Build(displays, vertical: true);

        var geometries = new DisplayGeometry[displays.Count];
        for (var i = 0; i < displays.Count; i++)
        {
            var display = displays[i];
            var left = horizontal.PixelOf(display.Points.Left);
            var top = vertical.PixelOf(display.Points.Top);

            // A capture of the display really has that many pixels, whatever the scale factors of the other displays.
            var width = Math.Max(1, (int)Math.Round(display.Points.Width * display.Scale));
            var height = Math.Max(1, (int)Math.Round(display.Points.Height * display.Scale));

            geometries[i] = new DisplayGeometry(display.Id, display.IsMain, display.Points, new Rectangle(left, top, left + width, top + height), display.Scale);
        }

        return geometries;
    }

    /// <summary>
    /// Returns the display a position in pixels is on, or the closest one when it is on none of them.
    /// </summary>
    public static DisplayGeometry FindFromPixels(DisplayGeometry[] geometries, int x, int y)
    {
        var closest = geometries[0];
        var closestDistance = double.MaxValue;

        foreach (var geometry in geometries)
        {
            var pixels = geometry.Pixels;
            if (x >= pixels.Left && x < pixels.Right && y >= pixels.Top && y < pixels.Bottom)
            {
                return geometry;
            }

            var distance = SquaredDistance(x, y, pixels.Left, pixels.Top, pixels.Right, pixels.Bottom);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = geometry;
            }
        }

        return closest;
    }

    /// <summary>
    /// Returns the display a position in points is on, or the closest one when it is on none of them.
    /// </summary>
    public static DisplayGeometry FindFromPoints(DisplayGeometry[] geometries, double x, double y)
    {
        var closest = geometries[0];
        var closestDistance = double.MaxValue;

        foreach (var geometry in geometries)
        {
            var points = geometry.Points;
            if (points.Contains(x, y))
            {
                return geometry;
            }

            var distance = SquaredDistance(x, y, points.Left, points.Top, points.Right, points.Bottom);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = geometry;
            }
        }

        return closest;
    }

    private static double SquaredDistance(double x, double y, double left, double top, double right, double bottom)
    {
        var horizontal = Math.Max(0, Math.Max(left - x, x - right));
        var vertical = Math.Max(0, Math.Max(top - y, y - bottom));
        return (horizontal * horizontal) + (vertical * vertical);
    }

    /// <summary>
    /// One axis of the desktop, as the edges of the displays in points and the pixel coordinate each of them is at.
    /// </summary>
    private readonly struct Axis
    {
        private readonly double[] _edges;
        private readonly int[] _pixels;

        private Axis(double[] edges, int[] pixels)
        {
            this._edges = edges;
            this._pixels = pixels;
        }

        public static Axis Build(IReadOnlyList<MacDisplay> displays, bool vertical)
        {
            var edges = CollectEdges(displays, vertical);
            var pixels = new int[edges.Length];

            // The main display is at the origin of the pixel coordinates, like the primary monitor of the other platforms.
            var origin = Array.BinarySearch(edges, StartOf(MainOf(displays), vertical));
            origin = origin < 0 ? 0 : origin;

            for (var i = origin + 1; i < edges.Length; i++)
            {
                pixels[i] = pixels[i - 1] + IntervalLength(displays, vertical, edges[i - 1], edges[i]);
            }

            for (var i = origin - 1; i >= 0; i--)
            {
                pixels[i] = pixels[i + 1] - IntervalLength(displays, vertical, edges[i], edges[i + 1]);
            }

            return new Axis(edges, pixels);
        }

        /// <summary>
        /// Returns the pixel coordinate an edge of a display is at.
        /// </summary>
        public int PixelOf(double edge)
        {
            var index = Array.BinarySearch(this._edges, edge);
            return this._pixels[index < 0 ? Math.Min(~index, this._pixels.Length - 1) : index];
        }

        private static double[] CollectEdges(IReadOnlyList<MacDisplay> displays, bool vertical)
        {
            var edges = new List<double>(displays.Count * 2);
            foreach (var display in displays)
            {
                edges.Add(StartOf(display, vertical));
                edges.Add(EndOf(display, vertical));
            }

            edges.Sort();

            var distinct = new List<double>(edges.Count);
            foreach (var edge in edges)
            {
                if (distinct.Count == 0 || distinct[^1] != edge)
                {
                    distinct.Add(edge);
                }
            }

            return distinct.ToArray();
        }

        /// <summary>
        /// Returns how many pixels an interval between two edges of displays is long: those of the display that shows
        /// the most pixels there. An interval no display covers, which is empty space between displays, is as long as
        /// it would be on the display with the most pixels per point.
        /// </summary>
        private static int IntervalLength(IReadOnlyList<MacDisplay> displays, bool vertical, double from, double to)
        {
            var scale = 0.0;
            var largestScale = 0.0;

            foreach (var display in displays)
            {
                largestScale = Math.Max(largestScale, display.Scale);

                // Edges are those of the displays, so a display covering any part of the interval covers all of it.
                if (StartOf(display, vertical) <= from && EndOf(display, vertical) >= to)
                {
                    scale = Math.Max(scale, display.Scale);
                }
            }

            if (scale <= 0)
            {
                scale = largestScale;
            }

            // Rounding up keeps a display from ever reaching into the pixels of the next one.
            return Math.Max(1, (int)Math.Ceiling(((to - from) * scale) - PixelTolerance));
        }

        private static MacDisplay MainOf(IReadOnlyList<MacDisplay> displays)
        {
            foreach (var display in displays)
            {
                if (display.IsMain)
                {
                    return display;
                }
            }

            return displays[0];
        }

        private static double StartOf(MacDisplay display, bool vertical) => vertical ? display.Points.Top : display.Points.Left;

        private static double EndOf(MacDisplay display, bool vertical) => vertical ? display.Points.Bottom : display.Points.Right;
    }
}
