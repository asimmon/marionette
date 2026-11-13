using System;
using System.IO;
using System.Reflection;

namespace Askaiser.Marionette;

/// <summary>
/// The neural networks used to read text on the screen. Marionette reads text with
/// <see href="https://github.com/PaddlePaddle/PaddleOCR">PP-OCR</see> models in ONNX format: a detection model
/// locates text and a recognition model reads its characters.
/// </summary>
/// <remarks>
/// The <see cref="Default"/> model is embedded in the library and reads languages written in Latin script, such as English,
/// French, German, Spanish, Portuguese, Italian, Dutch or Polish. Use <see cref="FromFiles"/> with another PP-OCR recognition
/// model to read other scripts, such as Cyrillic, Greek, Chinese, Japanese, Korean or Arabic.
/// </remarks>
public sealed class TextRecognitionModel
{
    private const string EmbeddedDetectionModel = "Askaiser.Marionette.Models.text-detection.onnx";
    private const string EmbeddedRecognitionModel = "Askaiser.Marionette.Models.text-recognition.onnx";
    private const string EmbeddedDictionary = "Askaiser.Marionette.Models.text-recognition.txt";

    private readonly string? _detectionModelPath;
    private readonly string? _recognitionModelPath;
    private readonly string? _dictionaryPath;

    private TextRecognitionModel(string? detectionModelPath, string? recognitionModelPath, string? dictionaryPath)
    {
        this._detectionModelPath = detectionModelPath;
        this._recognitionModelPath = recognitionModelPath;
        this._dictionaryPath = dictionaryPath;
    }

    /// <summary>
    /// Gets the model embedded in the library, which reads languages written in Latin script.
    /// </summary>
    public static TextRecognitionModel Default { get; } = new TextRecognitionModel(null, null, null);

    /// <summary>
    /// Creates a model from PP-OCR files in ONNX format.
    /// </summary>
    /// <param name="recognitionModelPath">The path of a PP-OCR text recognition model.</param>
    /// <param name="dictionaryPath">The path of the dictionary of the recognition model: a text file with one character per line.</param>
    /// <param name="detectionModelPath">The path of a PP-OCR text detection model, or null to use the embedded one, which is not specific to a language.</param>
    public static TextRecognitionModel FromFiles(string recognitionModelPath, string dictionaryPath, string? detectionModelPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recognitionModelPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(dictionaryPath);

        EnsureFileExists(recognitionModelPath, nameof(recognitionModelPath));
        EnsureFileExists(dictionaryPath, nameof(dictionaryPath));

        if (detectionModelPath != null)
        {
            EnsureFileExists(detectionModelPath, nameof(detectionModelPath));
        }

        return new TextRecognitionModel(detectionModelPath, recognitionModelPath, dictionaryPath);
    }

    internal byte[] ReadDetectionModel()
    {
        return this._detectionModelPath != null ? File.ReadAllBytes(this._detectionModelPath) : ReadEmbeddedResource(EmbeddedDetectionModel);
    }

    internal byte[] ReadRecognitionModel()
    {
        return this._recognitionModelPath != null ? File.ReadAllBytes(this._recognitionModelPath) : ReadEmbeddedResource(EmbeddedRecognitionModel);
    }

    /// <summary>
    /// Returns the characters the recognition model can output, indexed like its outputs:
    /// the first entry is the blank of the CTC decoding and the last one is the space.
    /// </summary>
    internal string[] ReadCharacters()
    {
        var content = this._dictionaryPath != null
            ? File.ReadAllText(this._dictionaryPath)
            : System.Text.Encoding.UTF8.GetString(ReadEmbeddedResource(EmbeddedDictionary));

        // Lines are not trimmed: some characters of a dictionary are whitespace.
        var lines = content.TrimStart('﻿').Split('\n');
        var count = lines.Length > 0 && lines[^1].Length == 0 ? lines.Length - 1 : lines.Length;

        var characters = new string[count + 2];
        characters[0] = string.Empty;
        for (var i = 0; i < count; i++)
        {
            characters[i + 1] = lines[i].TrimEnd('\r');
        }

        characters[^1] = " ";
        return characters;
    }

    private static void EnsureFileExists(string path, string parameterName)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(Messages.TextRecognition_Throw_FileNotFound.FormatInvariant(path), parameterName);
        }
    }

    private static byte[] ReadEmbeddedResource(string name)
    {
        using var stream = typeof(TextRecognitionModel).GetTypeInfo().Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(Messages.TextRecognition_Throw_ModelLoadFailed.FormatInvariant(name));

        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }
}
