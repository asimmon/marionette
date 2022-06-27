using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Tests;

internal sealed class FakeScreenshotWriter : IFileWriter
{
    private readonly ConcurrentDictionary<Guid, FakeSavedFailure> _savedFailures;

    public FakeScreenshotWriter()
    {
        this._savedFailures = new ConcurrentDictionary<Guid, FakeSavedFailure>();
    }

    public IReadOnlyList<FakeSavedFailure> SavedFailures
    {
        get => this._savedFailures.Values.ToList();
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The image will not be used after disposal.")]
    public Task SaveScreenshot(string path, byte[] screenshotBytes)
    {
        using var img = BitmapUtils.FromBytes(screenshotBytes);
        this._savedFailures.AddOrUpdate(Guid.NewGuid(), _ => new FakeSavedFailure(img.Width, img.Height, path), (_, x) => x);
        return Task.CompletedTask;
    }
}
