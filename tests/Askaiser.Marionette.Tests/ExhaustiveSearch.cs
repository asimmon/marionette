using TUnit.Assertions.Enums;
using System.Collections.Concurrent;
using Askaiser.Marionette.Imaging;
using Askaiser.Marionette.Recognition.Images;

namespace Askaiser.Marionette.Tests;

/// <summary>
/// The reference the image search is compared to: scoring every position of the screen, without any shortcut.
/// </summary>
internal static class ExhaustiveSearch
{
    public static SearchSurface CreateSurface(Image image)
    {
        return new SearchSurface(PixelBuffer.Wrap(image.Pixels.ToArray(), image.Width, image.Height), ownsPixels: true);
    }

    /// <summary>
    /// Asserts that a search returned exactly what an exhaustive search finds.
    /// </summary>
    public static async Task AssertSameAsExhaustiveAsync(SearchSurface surface, ImageTemplate template, IReadOnlyList<TemplateMatch> matches)
    {
        var expected = new ConcurrentBag<TemplateMatch>();
        var positionsX = surface.Width - template.Width + 1;
        var positionsY = surface.Height - template.Height + 1;

        Parallel.For(0, Math.Max(0, positionsY), y =>
        {
            for (var x = 0; x < positionsX; x++)
            {
                var score = TemplateMatcher.Score(surface, template, x, y);
                if (score >= template.Threshold - 1e-6)
                {
                    expected.Add(new TemplateMatch(x, y, score));
                }
            }
        });

        // One result per group of adjacent positions above the threshold: the best position of the group.
        var remaining = expected.ToDictionary(match => (match.X, match.Y));
        var peaks = new List<TemplateMatch>();
        while (remaining.Count > 0)
        {
            var seed = remaining.Values.First();
            var best = seed;
            var queue = new Queue<TemplateMatch>([seed]);
            remaining.Remove((seed.X, seed.Y));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                best = IsBetter(current, best) ? current : best;

                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (remaining.Remove((current.X + dx, current.Y + dy), out var neighbor))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            peaks.Add(best);
        }

        // Then, of two results that overlap by more than half in both directions, only the best one.
        var halfWidth = Math.Max(1, (template.Width + 1) / 2);
        var halfHeight = Math.Max(1, (template.Height + 1) / 2);
        var kept = new List<TemplateMatch>();
        foreach (var peak in peaks.OrderByDescending(peak => peak.Score).ThenBy(peak => peak.Y).ThenBy(peak => peak.X))
        {
            if (!kept.Exists(other => Math.Abs(peak.X - other.X) < halfWidth && Math.Abs(peak.Y - other.Y) < halfHeight))
            {
                kept.Add(peak);
            }
        }

        var expectedPositions = kept.OrderBy(match => match.Y).ThenBy(match => match.X).Select(match => (match.X, match.Y)).ToList();
        var actualPositions = matches.Select(match => (match.X, match.Y)).ToList();
        await Assert.That(actualPositions).IsEquivalentTo(expectedPositions, CollectionOrdering.Matching);

        foreach (var match in matches)
        {
            await Assert.That(match.Score).IsEqualTo(kept.Single(other => other.X == match.X && other.Y == match.Y).Score).Within(1e-9);
        }
    }

    private static bool IsBetter(TemplateMatch a, TemplateMatch b)
    {
        if (a.Score != b.Score)
        {
            return a.Score > b.Score;
        }

        return a.Y != b.Y ? a.Y < b.Y : a.X < b.X;
    }
}
