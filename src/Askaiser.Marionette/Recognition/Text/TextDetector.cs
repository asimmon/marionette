using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Askaiser.Marionette.Imaging;
using Microsoft.ML.OnnxRuntime;

namespace Askaiser.Marionette.Recognition.Text;

/// <summary>
/// Locates text on an image with a differentiable binarization (DB) neural network.
/// The network outputs, for every pixel, the probability that it belongs to the core of a text line.
/// </summary>
internal sealed class TextDetector : IDisposable
{
    /// <summary>Probability above which a pixel is considered part of a text line.</summary>
    private const float PixelThreshold = 0.3f;

    /// <summary>Minimum average probability of a group of pixels to be kept as a text line.</summary>
    private const float BoxThreshold = 0.5f;

    /// <summary>The network only marks the core of a text line. Its bounds are grown back by this factor.</summary>
    private const float UnclipRatio = 1.6f;

    /// <summary>The network reduces its input several times by a factor of two, so the dimensions of the input must be multiples of 32.</summary>
    private const int SizeMultiple = 32;

    private const int MinBoxSide = 3;

    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string[] _outputNames;

    public TextDetector(InferenceSession session)
    {
        this._session = session;
        this._inputName = session.InputNames[0];
        this._outputNames = [session.OutputNames[0]];
    }

    /// <summary>
    /// Returns the bounds of the text lines of an image.
    /// </summary>
    /// <param name="pixels">The image.</param>
    /// <param name="scale">The factor the image is resized by before being analyzed. Use a value lower than 1 on high pixel density screens, where text is large.</param>
    public List<TextBox> Detect(PixelBuffer pixels, float scale)
    {
        var scaledWidth = Math.Max(1, (int)MathF.Round(pixels.Width * scale));
        var scaledHeight = Math.Max(1, (int)MathF.Round(pixels.Height * scale));
        var inputWidth = RoundUp(scaledWidth);
        var inputHeight = RoundUp(scaledHeight);
        var planeSize = inputWidth * inputHeight;

        var input = ArrayPool<float>.Shared.Rent(planeSize * 3);
        try
        {
            FillInput(pixels, input, inputWidth, inputHeight, scaledWidth, scaledHeight);

            using var inputValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, input.AsMemory(0, planeSize * 3), [1, 3, inputHeight, inputWidth]);
            using var runOptions = new RunOptions();
            using var outputs = this._session.Run(runOptions, [this._inputName], [inputValue], this._outputNames);

            var probabilities = outputs[0].GetTensorDataAsSpan<float>();
            var boxes = ExtractBoxes(probabilities, inputWidth, inputHeight, scaledWidth, scaledHeight);

            // Back to the coordinates of the original image.
            var horizontalRatio = (float)pixels.Width / scaledWidth;
            var verticalRatio = (float)pixels.Height / scaledHeight;
            for (var i = 0; i < boxes.Count; i++)
            {
                var box = boxes[i];
                boxes[i] = new TextBox(
                    Math.Clamp((int)MathF.Floor(box.Left * horizontalRatio), 0, pixels.Width),
                    Math.Clamp((int)MathF.Floor(box.Top * verticalRatio), 0, pixels.Height),
                    Math.Clamp((int)MathF.Ceiling(box.Right * horizontalRatio), 0, pixels.Width),
                    Math.Clamp((int)MathF.Ceiling(box.Bottom * verticalRatio), 0, pixels.Height));
            }

            boxes.RemoveAll(static box => box.Width < MinBoxSide || box.Height < MinBoxSide);
            return boxes;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(input);
        }
    }

    public void Dispose()
    {
        this._session.Dispose();
    }

    private static int RoundUp(int value)
    {
        return (value + SizeMultiple - 1) / SizeMultiple * SizeMultiple;
    }

    /// <summary>
    /// Writes the image as three normalized planes (blue, green, red), resized and padded to the network input size.
    /// </summary>
    private static void FillInput(PixelBuffer pixels, float[] input, int inputWidth, int inputHeight, int scaledWidth, int scaledHeight)
    {
        var planeSize = inputWidth * inputHeight;
        var sameSize = scaledWidth == pixels.Width && scaledHeight == pixels.Height;

        // Padding is filled with the value of a black pixel, like the rest of the PaddleOCR tooling does.
        const float blueScale = 1f / (255f * 0.229f), greenScale = 1f / (255f * 0.224f), redScale = 1f / (255f * 0.225f);
        const float blueOffset = -0.485f / 0.229f, greenOffset = -0.456f / 0.224f, redOffset = -0.406f / 0.225f;

        Parallel.For(0, inputHeight, y =>
        {
            var blue = input.AsSpan(y * inputWidth, inputWidth);
            var green = input.AsSpan(planeSize + (y * inputWidth), inputWidth);
            var red = input.AsSpan((2 * planeSize) + (y * inputWidth), inputWidth);

            if (y >= scaledHeight)
            {
                blue.Fill(blueOffset);
                green.Fill(greenOffset);
                red.Fill(redOffset);
                return;
            }

            if (sameSize)
            {
                var row = MemoryMarshal.Cast<byte, uint>(pixels.GetRow(y));
                for (var x = 0; x < row.Length; x++)
                {
                    var p = row[x];
                    blue[x] = ((byte)p * blueScale) + blueOffset;
                    green[x] = ((byte)(p >> 8) * greenScale) + greenOffset;
                    red[x] = ((byte)(p >> 16) * redScale) + redOffset;
                }
            }
            else
            {
                // Bilinear interpolation.
                var sourceY = Math.Clamp((((y + 0.5f) * pixels.Height) / scaledHeight) - 0.5f, 0, pixels.Height - 1);
                var y0 = (int)sourceY;
                var y1 = Math.Min(y0 + 1, pixels.Height - 1);
                var weightY = sourceY - y0;
                var top = pixels.GetRow(y0);
                var bottom = pixels.GetRow(y1);

                for (var x = 0; x < scaledWidth; x++)
                {
                    var sourceX = Math.Clamp((((x + 0.5f) * pixels.Width) / scaledWidth) - 0.5f, 0, pixels.Width - 1);
                    var x0 = (int)sourceX;
                    var x1 = Math.Min(x0 + 1, pixels.Width - 1);
                    var weightX = sourceX - x0;

                    blue[x] = (Interpolate(top, bottom, x0 * 4, x1 * 4, weightX, weightY) * blueScale) + blueOffset;
                    green[x] = (Interpolate(top, bottom, (x0 * 4) + 1, (x1 * 4) + 1, weightX, weightY) * greenScale) + greenOffset;
                    red[x] = (Interpolate(top, bottom, (x0 * 4) + 2, (x1 * 4) + 2, weightX, weightY) * redScale) + redOffset;
                }
            }

            blue.Slice(scaledWidth).Fill(blueOffset);
            green.Slice(scaledWidth).Fill(greenOffset);
            red.Slice(scaledWidth).Fill(redOffset);
        });
    }

    private static float Interpolate(ReadOnlySpan<byte> top, ReadOnlySpan<byte> bottom, int i0, int i1, float weightX, float weightY)
    {
        var upper = top[i0] + ((top[i1] - top[i0]) * weightX);
        var lower = bottom[i0] + ((bottom[i1] - bottom[i0]) * weightX);
        return upper + ((lower - upper) * weightY);
    }

    /// <summary>
    /// Groups connected pixels of high probability and returns the bounds of each group, grown back to the size of the text.
    /// Text on a screen is horizontal, so axis-aligned bounds are enough.
    /// </summary>
    private static List<TextBox> ExtractBoxes(ReadOnlySpan<float> probabilities, int width, int height, int validWidth, int validHeight)
    {
        var boxes = new List<TextBox>();
        var visited = ArrayPool<bool>.Shared.Rent(width * height);
        var stack = new Stack<int>();

        try
        {
            visited.AsSpan(0, width * height).Clear();

            for (var y = 0; y < validHeight; y++)
            {
                for (var x = 0; x < validWidth; x++)
                {
                    var start = (y * width) + x;
                    if (visited[start] || probabilities[start] <= PixelThreshold)
                    {
                        continue;
                    }

                    int left = x, right = x, top = y, bottom = y, count = 0;
                    var total = 0.0;

                    visited[start] = true;
                    stack.Push(start);

                    while (stack.Count > 0)
                    {
                        var index = stack.Pop();
                        var px = index % width;
                        var py = index / width;

                        left = Math.Min(left, px);
                        right = Math.Max(right, px);
                        top = Math.Min(top, py);
                        bottom = Math.Max(bottom, py);
                        total += probabilities[index];
                        count++;

                        if (px > 0)
                        {
                            Visit(index - 1, probabilities, visited, stack);
                        }

                        if (px < validWidth - 1)
                        {
                            Visit(index + 1, probabilities, visited, stack);
                        }

                        if (py > 0)
                        {
                            Visit(index - width, probabilities, visited, stack);
                        }

                        if (py < validHeight - 1)
                        {
                            Visit(index + width, probabilities, visited, stack);
                        }
                    }

                    var boxWidth = right - left + 1;
                    var boxHeight = bottom - top + 1;
                    if (boxWidth < MinBoxSide || boxHeight < MinBoxSide || total / count < BoxThreshold)
                    {
                        continue;
                    }

                    var margin = boxWidth * boxHeight * UnclipRatio / (2f * (boxWidth + boxHeight));
                    boxes.Add(new TextBox(
                        Math.Max(0, (int)MathF.Floor(left - margin)),
                        Math.Max(0, (int)MathF.Floor(top - margin)),
                        Math.Min(validWidth, (int)MathF.Ceiling(right + 1 + margin)),
                        Math.Min(validHeight, (int)MathF.Ceiling(bottom + 1 + margin))));
                }
            }
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(visited);
        }

        return boxes;
    }

    private static void Visit(int index, ReadOnlySpan<float> probabilities, bool[] visited, Stack<int> stack)
    {
        if (!visited[index] && probabilities[index] > PixelThreshold)
        {
            visited[index] = true;
            stack.Push(index);
        }
    }
}
