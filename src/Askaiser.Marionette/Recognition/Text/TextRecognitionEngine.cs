using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Askaiser.Marionette.Imaging;
using Microsoft.ML.OnnxRuntime;

namespace Askaiser.Marionette.Recognition.Text;

/// <summary>
/// Reads the text of an image: locates text lines, reads their characters, then joins the lines of a same row.
/// </summary>
/// <remarks>
/// Neural networks are loaded the first time text is read. Reading characters is the most expensive step, and consecutive
/// screenshots mostly show the same text, so what was read is remembered by the pixels it was read from: only text
/// that changed on the screen is read again.
/// </remarks>
internal sealed class TextRecognitionEngine : IDisposable
{
    private const int MaxCachedLines = 8192;

    private readonly TextRecognitionModel _model;
    private readonly Lock _gate = new();
    private readonly Dictionary<LineKey, TextLine?> _cache = [];
    private TextDetector? _detector;
    private TextLineReader? _reader;
    private bool _disposed;

    public TextRecognitionEngine(TextRecognitionModel model)
    {
        this._model = model;
    }

    /// <summary>
    /// Reads the text of an image.
    /// </summary>
    /// <param name="pixels">The image.</param>
    /// <param name="detectionScale">The factor the image is resized by to locate text, see <see cref="TextDetector.Detect"/>.</param>
    public List<TextLine> Read(PixelBuffer pixels, float detectionScale)
    {
        this.EnsureLoaded();

        var boxes = this._detector!.Detect(pixels, detectionScale);
        var lines = new TextLine?[boxes.Count];
        var keys = new LineKey[boxes.Count];
        var unknownBoxes = new List<TextBox>();
        var unknownIndices = new List<int>();

        lock (this._gate)
        {
            for (var i = 0; i < boxes.Count; i++)
            {
                keys[i] = LineKey.Create(pixels, boxes[i]);
                if (this._cache.TryGetValue(keys[i], out var cached))
                {
                    lines[i] = cached == null ? null : Translate(cached, boxes[i].Left, boxes[i].Top);
                }
                else
                {
                    unknownBoxes.Add(boxes[i]);
                    unknownIndices.Add(i);
                }
            }
        }

        if (unknownBoxes.Count > 0)
        {
            var read = this._reader!.Read(pixels, unknownBoxes);

            lock (this._gate)
            {
                if (this._cache.Count + read.Length > MaxCachedLines)
                {
                    this._cache.Clear();
                }

                for (var i = 0; i < read.Length; i++)
                {
                    var index = unknownIndices[i];
                    lines[index] = read[i];

                    // Lines are remembered relative to their box, since the same text can later show up elsewhere on the screen.
                    this._cache[keys[index]] = read[i] == null ? null : Translate(read[i]!, -boxes[index].Left, -boxes[index].Top);
                }
            }
        }

        return TextFinder.JoinRows(lines);
    }

    public void Dispose()
    {
        lock (this._gate)
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
            this._detector?.Dispose();
            this._reader?.Dispose();
        }
    }

    private static TextLine Translate(TextLine line, int offsetX, int offsetY)
    {
        var lefts = new float[line.CharacterLefts.Length];
        var rights = new float[line.CharacterRights.Length];
        for (var i = 0; i < lefts.Length; i++)
        {
            lefts[i] = line.CharacterLefts[i] + offsetX;
            rights[i] = line.CharacterRights[i] + offsetX;
        }

        return new TextLine(line.Text, lefts, rights, line.Top + offsetY, line.Bottom + offsetY, line.Confidence);
    }

    private void EnsureLoaded()
    {
        if (Volatile.Read(ref this._reader) != null)
        {
            return;
        }

        lock (this._gate)
        {
            ObjectDisposedException.ThrowIf(this._disposed, this);

            if (this._reader != null)
            {
                return;
            }

            try
            {
                using var options = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR,
                };

                this._detector = new TextDetector(new InferenceSession(this._model.ReadDetectionModel(), options));
                Volatile.Write(ref this._reader, new TextLineReader(new InferenceSession(this._model.ReadRecognitionModel(), options), this._model.ReadCharacters()));
            }
            catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException or EntryPointNotFoundException or BadImageFormatException)
            {
                throw new PlatformNotSupportedException(Messages.TextRecognition_Throw_Unavailable.FormatInvariant(RuntimeInformation.RuntimeIdentifier), ex);
            }
            catch (OnnxRuntimeException ex)
            {
                throw new InvalidOperationException(Messages.TextRecognition_Throw_ModelLoadFailed.FormatInvariant(ex.Message), ex);
            }
        }
    }

    /// <summary>
    /// Identifies a text line by its size and a hash of its pixels.
    /// </summary>
    private readonly record struct LineKey(int Width, int Height, ulong Hash)
    {
        public static LineKey Create(PixelBuffer pixels, TextBox box)
        {
            var hash = 0xCBF29CE484222325UL;
            var rowLength = box.Width * PixelBuffer.BytesPerPixel;

            for (var y = box.Top; y < box.Bottom; y++)
            {
                var row = pixels.GetRow(y).Slice(box.Left * PixelBuffer.BytesPerPixel, rowLength);
                var words = MemoryMarshal.Cast<byte, ulong>(row);

                foreach (var word in words)
                {
                    hash = (hash ^ word) * 0x9E3779B97F4A7C15UL;
                    hash ^= hash >> 29;
                }

                foreach (var remaining in row.Slice(words.Length * sizeof(ulong)))
                {
                    hash = (hash ^ remaining) * 0x100000001B3UL;
                }
            }

            return new LineKey(box.Width, box.Height, hash);
        }
    }
}
