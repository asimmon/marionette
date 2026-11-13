using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Askaiser.Marionette.Imaging;
using Microsoft.ML.OnnxRuntime;

namespace Askaiser.Marionette.Recognition.Text;

/// <summary>
/// Reads the characters of text lines located by a <see cref="TextDetector"/>, with a neural network trained with a
/// connectionist temporal classification (CTC) loss: for each vertical slice of a line it outputs the probability of every character.
/// </summary>
internal sealed class TextLineReader : IDisposable
{
    /// <summary>Height of the images the network reads.</summary>
    private const int LineHeight = 48;

    private const int MinLineWidth = 16;

    /// <summary>Lines of similar width are read together. This bounds how much wider than its content a batch can be.</summary>
    private const float MaxBatchWidthRatio = 1.25f;

    private const int MaxBatchSize = 8;

    /// <summary>Lines read with less confidence than this are most likely not text.</summary>
    private const float MinConfidence = 0.5f;

    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string[] _outputNames;
    private readonly string[] _characters;

    /// <param name="session">The recognition network.</param>
    /// <param name="characters">The characters the network knows, in the order of its outputs. Index zero is the CTC blank.</param>
    public TextLineReader(InferenceSession session, string[] characters)
    {
        this._session = session;
        this._inputName = session.InputNames[0];
        this._outputNames = [session.OutputNames[0]];
        this._characters = characters;
    }

    /// <summary>
    /// Reads text lines. The returned array has one entry per box, null where no text could be read.
    /// </summary>
    public TextLine?[] Read(PixelBuffer pixels, IReadOnlyList<TextBox> boxes)
    {
        var lines = new TextLine?[boxes.Count];
        if (boxes.Count == 0)
        {
            return lines;
        }

        // Read lines of similar width together, narrowest first.
        var order = new int[boxes.Count];
        var widths = new int[boxes.Count];
        for (var i = 0; i < boxes.Count; i++)
        {
            order[i] = i;
            widths[i] = Math.Max(MinLineWidth, (int)MathF.Ceiling((float)LineHeight * boxes[i].Width / Math.Max(1, boxes[i].Height)));
        }

        Array.Sort(order, (a, b) => widths[a].CompareTo(widths[b]));

        for (var start = 0; start < order.Length;)
        {
            var end = start + 1;
            while (end < order.Length && end - start < MaxBatchSize && widths[order[end]] <= widths[order[start]] * MaxBatchWidthRatio)
            {
                end++;
            }

            this.ReadBatch(pixels, boxes, order.AsSpan(start, end - start), widths, lines);
            start = end;
        }

        return lines;
    }

    public void Dispose()
    {
        this._session.Dispose();
    }

    private void ReadBatch(PixelBuffer pixels, IReadOnlyList<TextBox> boxes, ReadOnlySpan<int> batch, int[] widths, TextLine?[] lines)
    {
        var batchWidth = 0;
        foreach (var index in batch)
        {
            batchWidth = Math.Max(batchWidth, widths[index]);
        }

        var count = batch.Length;
        var planeSize = LineHeight * batchWidth;
        var inputLength = count * 3 * planeSize;
        var input = ArrayPool<float>.Shared.Rent(inputLength);

        try
        {
            // Lines narrower than the batch are padded on the right with the value of a black pixel.
            input.AsSpan(0, inputLength).Clear();

            var indices = batch.ToArray();
            Parallel.For(0, count, i => FillLine(pixels, boxes[indices[i]], widths[indices[i]], input.AsSpan(i * 3 * planeSize, 3 * planeSize), batchWidth));

            using var inputValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, input.AsMemory(0, inputLength), [count, 3, LineHeight, batchWidth]);
            using var runOptions = new RunOptions();
            using var outputs = this._session.Run(runOptions, [this._inputName], [inputValue], this._outputNames);

            var shape = outputs[0].GetTensorTypeAndShape().Shape;
            var steps = (int)shape[1];
            var classes = (int)shape[2];
            var probabilities = outputs[0].GetTensorDataAsSpan<float>();

            for (var i = 0; i < count; i++)
            {
                var index = batch[i];
                lines[index] = this.Decode(probabilities.Slice(i * steps * classes, steps * classes), steps, classes, boxes[index], widths[index], batchWidth);
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(input);
        }
    }

    /// <summary>
    /// Resizes a text line to the height the network expects and writes it as three normalized planes (blue, green, red).
    /// </summary>
    private static void FillLine(PixelBuffer pixels, TextBox box, int width, Span<float> destination, int batchWidth)
    {
        var planeSize = LineHeight * batchWidth;
        const float scale = 1f / 127.5f;

        for (var y = 0; y < LineHeight; y++)
        {
            var sourceY = Math.Clamp(box.Top + (((y + 0.5f) * box.Height) / LineHeight) - 0.5f, 0, pixels.Height - 1);
            var y0 = (int)sourceY;
            var y1 = Math.Min(y0 + 1, pixels.Height - 1);
            var weightY = sourceY - y0;
            var top = pixels.GetRow(y0);
            var bottom = pixels.GetRow(y1);

            var blue = destination.Slice(y * batchWidth, width);
            var green = destination.Slice(planeSize + (y * batchWidth), width);
            var red = destination.Slice((2 * planeSize) + (y * batchWidth), width);

            for (var x = 0; x < width; x++)
            {
                var sourceX = Math.Clamp(box.Left + (((x + 0.5f) * box.Width) / width) - 0.5f, 0, pixels.Width - 1);
                var x0 = (int)sourceX;
                var x1 = Math.Min(x0 + 1, pixels.Width - 1);
                var weightX = sourceX - x0;

                blue[x] = (Interpolate(top, bottom, x0 * 4, x1 * 4, weightX, weightY) * scale) - 1f;
                green[x] = (Interpolate(top, bottom, (x0 * 4) + 1, (x1 * 4) + 1, weightX, weightY) * scale) - 1f;
                red[x] = (Interpolate(top, bottom, (x0 * 4) + 2, (x1 * 4) + 2, weightX, weightY) * scale) - 1f;
            }
        }
    }

    private static float Interpolate(ReadOnlySpan<byte> top, ReadOnlySpan<byte> bottom, int i0, int i1, float weightX, float weightY)
    {
        var upper = top[i0] + ((top[i1] - top[i0]) * weightX);
        var lower = bottom[i0] + ((bottom[i1] - bottom[i0]) * weightX);
        return upper + ((lower - upper) * weightY);
    }

    /// <summary>
    /// Turns the output of the network into text: the most likely character of each slice is kept, then repeated characters
    /// and blanks are removed. The slice a character first appears in tells where it is located in the line.
    /// </summary>
    private TextLine? Decode(ReadOnlySpan<float> probabilities, int steps, int classes, TextBox box, int width, int batchWidth)
    {
        var text = new StringBuilder();
        var centers = new List<float>();
        var confidence = 0f;
        var previous = 0;

        // Horizontal size of a slice in the original image. Slices cover the whole batch width, padding included.
        var stepWidth = (float)batchWidth / steps * box.Width / width;

        for (var step = 0; step < steps; step++)
        {
            var slice = probabilities.Slice(step * classes, classes);
            var best = 0;
            var bestProbability = slice[0];
            for (var c = 1; c < slice.Length; c++)
            {
                if (slice[c] > bestProbability)
                {
                    bestProbability = slice[c];
                    best = c;
                }
            }

            if (best != 0 && best != previous && best < this._characters.Length)
            {
                var character = this._characters[best];
                var center = Math.Min(box.Left + ((step + 0.5f) * stepWidth), box.Right);

                // A few entries of the dictionary are made of several UTF-16 code units: they share the same location.
                foreach (var codeUnit in character)
                {
                    text.Append(codeUnit);
                    centers.Add(center);
                }

                confidence += bestProbability;
            }

            previous = best;
        }

        if (text.Length == 0)
        {
            return null;
        }

        confidence /= centers.Count;
        if (confidence < MinConfidence)
        {
            return null;
        }

        // Characters are delimited halfway between the centers of their neighbors. The first and the last one
        // are given the average character width, which keeps the margins of the box out of the text bounds.
        var count = centers.Count;
        var averageWidth = count > 1 ? (centers[count - 1] - centers[0]) / (count - 1) : 2 * stepWidth;
        var lefts = new float[count];
        var rights = new float[count];
        for (var i = 0; i < count; i++)
        {
            lefts[i] = i == 0 ? Math.Max(box.Left, centers[0] - (averageWidth / 2)) : (centers[i - 1] + centers[i]) / 2;
            rights[i] = i == count - 1 ? Math.Min(box.Right, centers[i] + (averageWidth / 2)) : (centers[i] + centers[i + 1]) / 2;
        }

        return new TextLine(text.ToString(), lefts, rights, box.Top, box.Bottom, confidence);
    }
}
