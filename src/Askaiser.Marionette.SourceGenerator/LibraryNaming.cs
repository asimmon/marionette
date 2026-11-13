using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Askaiser.Marionette.SourceGenerator;

/// <summary>
/// Turns file names into the names and options of generated properties.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Directories, and <c>--</c> in a file name, create nested libraries: <c>pages/login--title.png</c> is <c>Pages.Login.Title</c>.</item>
/// <item>A dash separates words: <c>ok-button.png</c> is <c>OkButton</c>.</item>
/// <item>An underscore introduces an option: <c>_gs</c> for grayscale, <c>_0.8</c> for a threshold, and a final <c>_2</c> for the position in an array of images.</item>
/// </list>
/// </remarks>
internal static class LibraryNaming
{
    private static readonly string[] LibrarySeparator = ["--"];

    private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp" };

    public static bool IsImage(string path)
    {
        return ImageExtensions.Contains(Path.GetExtension(path));
    }

    /// <summary>
    /// Parses the relative path of an image. Returns null when the name of the file does not contain anything usable as a property name.
    /// </summary>
    public static ParsedName? Parse(string relativePath)
    {
        var segments = relativePath.Split('/');
        var libraries = new List<string>();

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var name = ToPropertyName(segments[i]);
            if (name.Length == 0)
            {
                return null;
            }

            libraries.Add(name);
        }

        var parts = Path.GetFileNameWithoutExtension(segments[segments.Length - 1])
            .Split(LibrarySeparator, StringSplitOptions.None)
            .Select(static part => part.Trim())
            .Where(static part => part.Length > 0)
            .ToArray();

        if (parts.Length == 0)
        {
            return null;
        }

        for (var i = 0; i < parts.Length - 1; i++)
        {
            var name = ToPropertyName(parts[i]);
            if (name.Length == 0)
            {
                return null;
            }

            libraries.Add(name);
        }

        var options = parts[parts.Length - 1].Split('_').Select(static option => option.Trim()).Where(static option => option.Length > 0).ToArray();
        if (options.Length == 0)
        {
            return null;
        }

        var propertyName = ToPropertyName(options[0]);
        if (propertyName.Length == 0)
        {
            return null;
        }

        var threshold = Constants.DefaultThreshold;
        var grayscale = false;
        var groupIndex = 0;

        for (var i = 1; i < options.Length; i++)
        {
            if (string.Equals(options[i], "gs", StringComparison.OrdinalIgnoreCase))
            {
                grayscale = true;
            }
            else if (TryParseThreshold(options[i], out var parsedThreshold))
            {
                threshold = parsedThreshold;
            }
        }

        if (options.Length > 1 && TryParseGroupIndex(options[options.Length - 1], out var parsedIndex))
        {
            groupIndex = parsedIndex;
        }

        return new ParsedName(libraries, propertyName, groupIndex, threshold, grayscale);
    }

    /// <summary>
    /// Converts a file or directory name to a C# identifier: dashes separate words, which are capitalized and joined;
    /// other characters that are not letters or digits are dropped.
    /// </summary>
    public static string ToPropertyName(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var word in text.Split('-'))
        {
            var isFirstCharacter = true;
            foreach (var c in word)
            {
                if (c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
                {
                    builder.Append(isFirstCharacter ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                    isFirstCharacter = false;
                }
            }
        }

        // An identifier cannot start with a digit.
        if (builder.Length > 0 && char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    /// <summary>
    /// A threshold is written <c>1.0</c>, or <c>0.</c> followed by one to four digits.
    /// </summary>
    private static bool TryParseThreshold(string text, out decimal threshold)
    {
        threshold = 0;

        var isValid = text == "1.0" || (text.Length is >= 3 and <= 6 && text.StartsWith("0.", StringComparison.Ordinal) && text.Skip(2).All(char.IsDigit));
        return isValid && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out threshold);
    }

    /// <summary>
    /// A position is a positive integer without leading zero. Zero is the default and does not need to be parsed.
    /// </summary>
    private static bool TryParseGroupIndex(string text, out int index)
    {
        index = 0;
        return text.Length is > 0 and < 10 && text[0] != '0' && text.All(char.IsDigit) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }
}
