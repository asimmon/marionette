using System;
using System.Collections.Generic;
using System.Text;

namespace Askaiser.Marionette.Recognition.Text;

/// <summary>
/// Looks for a piece of text among the text read on the screen.
/// </summary>
internal static class TextFinder
{
    /// <summary>Two pieces of text belong to the same row when they overlap vertically by at least this share of the smallest one.</summary>
    private const float MinVerticalOverlap = 0.5f;

    /// <summary>Two pieces of text of the same row are joined when they are closer than this many times their height.</summary>
    private const float MaxGapInHeights = 3f;

    /// <summary>Two joined pieces of text are separated by a space when the gap between them exceeds this share of a character width.</summary>
    private const float MinSpaceInCharacters = 0.3f;

    /// <summary>
    /// Joins the pieces of text that are next to each other on the same row, from left to right, with a space.
    /// Text detection tends to split a row where words are far apart, such as the entries of a menu bar:
    /// joining them back lets a search span several of them.
    /// </summary>
    public static List<TextLine> JoinRows(IEnumerable<TextLine?> lines)
    {
        var remaining = new List<TextLine>();
        foreach (var line in lines)
        {
            if (line != null)
            {
                remaining.Add(line);
            }
        }

        remaining.Sort(static (a, b) => a.Left.CompareTo(b.Left));

        var rows = new List<TextLine>();
        var used = new bool[remaining.Count];

        for (var i = 0; i < remaining.Count; i++)
        {
            if (used[i])
            {
                continue;
            }

            used[i] = true;
            var current = remaining[i];
            List<TextLine>? parts = null;

            for (var j = i + 1; j < remaining.Count; j++)
            {
                if (used[j] || !AreAdjacent(current, remaining[j]))
                {
                    continue;
                }

                used[j] = true;
                (parts ??= [remaining[i]]).Add(remaining[j]);
                current = remaining[j];
            }

            rows.Add(parts == null ? current : Join(parts));
        }

        rows.Sort(static (a, b) => a.Top != b.Top ? a.Top.CompareTo(b.Top) : a.Left.CompareTo(b.Left));
        return rows;
    }

    /// <summary>
    /// Returns the bounds of every occurrence of a text. Whitespace is normalized on both sides: any sequence of
    /// whitespace characters is equivalent to a single space.
    /// </summary>
    public static List<Rectangle> Find(IReadOnlyList<TextLine> rows, string searchedText, bool ignoreCase)
    {
        var locations = new List<Rectangle>();
        var needle = NormalizeWhitespace(searchedText, map: null);
        if (needle.Length == 0)
        {
            return locations;
        }

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var map = new List<int>();

        foreach (var row in rows)
        {
            map.Clear();
            var haystack = NormalizeWhitespace(row.Text, map);

            for (var start = haystack.IndexOf(needle, comparison); start >= 0; start = haystack.IndexOf(needle, start + needle.Length, comparison))
            {
                var first = map[start];
                var last = map[start + needle.Length - 1];

                var left = (int)MathF.Floor(row.CharacterLefts[first]);
                var right = (int)MathF.Ceiling(row.CharacterRights[last]);
                locations.Add(new Rectangle(left, row.Top, Math.Max(left + 1, right), Math.Max(row.Top + 1, row.Bottom)));

                if (start + needle.Length >= haystack.Length)
                {
                    break;
                }
            }
        }

        return locations;
    }

    private static bool AreAdjacent(TextLine left, TextLine right)
    {
        var overlap = Math.Min(left.Bottom, right.Bottom) - Math.Max(left.Top, right.Top);
        var smallestHeight = Math.Min(left.Bottom - left.Top, right.Bottom - right.Top);
        if (smallestHeight <= 0 || overlap < smallestHeight * MinVerticalOverlap)
        {
            return false;
        }

        var gap = right.Left - left.Right;
        return gap > -smallestHeight && gap <= smallestHeight * MaxGapInHeights;
    }

    private static TextLine Join(List<TextLine> parts)
    {
        var text = new StringBuilder();
        var lefts = new List<float>();
        var rights = new List<float>();
        int top = int.MaxValue, bottom = int.MinValue;
        var confidence = float.MaxValue;

        foreach (var part in parts)
        {
            if (text.Length > 0)
            {
                // Detected boxes sometimes overlap, in which case the end of the left piece was also read as the beginning
                // of the right one: drop the characters of the left piece that are inside the right one.
                while (text.Length > 0 && (lefts[^1] + rights[^1]) / 2 >= part.Left)
                {
                    text.Length--;
                    lefts.RemoveAt(lefts.Count - 1);
                    rights.RemoveAt(rights.Count - 1);
                }

                // Both pieces are separated by a space, unless they touch each other like the letters of a word.
                var gapLeft = rights.Count > 0 ? rights[^1] : part.Left;
                var gapRight = Math.Max(gapLeft, part.Left);
                var characterWidth = (part.Right - part.Left) / Math.Max(1, part.Text.Length);
                if (text.Length > 0 && text[^1] != ' ' && gapRight - gapLeft > characterWidth * MinSpaceInCharacters)
                {
                    text.Append(' ');
                    lefts.Add(gapLeft);
                    rights.Add(gapRight);
                }
            }

            text.Append(part.Text);
            lefts.AddRange(part.CharacterLefts);
            rights.AddRange(part.CharacterRights);
            top = Math.Min(top, part.Top);
            bottom = Math.Max(bottom, part.Bottom);
            confidence = Math.Min(confidence, part.Confidence);
        }

        return new TextLine(text.ToString(), [.. lefts], [.. rights], top, bottom, confidence);
    }

    /// <summary>
    /// Trims the text and replaces every sequence of whitespace with a single space.
    /// When a map is provided, it receives the index, in the original text, of every character of the result.
    /// </summary>
    private static string NormalizeWhitespace(string text, List<int>? map)
    {
        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;

        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                map?.Add(i - 1);
                pendingSpace = false;
            }

            builder.Append(text[i]);
            map?.Add(i);
        }

        return builder.ToString();
    }
}
