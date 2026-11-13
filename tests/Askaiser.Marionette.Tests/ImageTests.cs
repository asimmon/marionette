using TUnit.Assertions.Enums;

namespace Askaiser.Marionette.Tests;

public class ImageTests
{
    [Test]
    public async Task FromBgra32_CopiesPixels()
    {
        byte[] pixels = [1, 2, 3, 255, 4, 5, 6, 255];
        var image = Image.FromBgra32(2, 1, pixels);
        pixels[0] = 99;

        await Assert.That(image.Width).IsEqualTo(2);
        await Assert.That(image.Height).IsEqualTo(1);
        await Assert.That(image.Pixels.ToArray()).IsEquivalentTo(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task FromRgba32_SwapsRedAndBlue()
    {
        var image = Image.FromRgba32(1, 1, [10, 20, 30, 255]);
        await Assert.That(image.Pixels.ToArray()).IsEquivalentTo(new byte[] { 30, 20, 10, 255 }, CollectionOrdering.Matching);
    }

    [Test]
    [Arguments(0, 1)]
    [Arguments(1, 0)]
    [Arguments(-1, 1)]
    public async Task FromBgra32_WhenInvalidDimensions_Throws(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Image.FromBgra32(width, height, []));
    }

    [Test]
    public async Task FromBgra32_WhenPixelCountDoesNotMatch_Throws()
    {
        Assert.Throws<ArgumentException>(() => Image.FromBgra32(2, 2, new byte[15]));
    }

    [Test]
    [Arguments(1, 1, false)]
    [Arguments(17, 5, false)]
    [Arguments(64, 33, false)]
    [Arguments(17, 5, true)]
    [Arguments(200, 120, true)]
    public async Task Png_RoundTrip_PreservesPixels(int width, int height, bool withTransparency)
    {
        var random = new Random(width * height);
        var pixels = new byte[width * height * 4];
        random.NextBytes(pixels);

        if (!withTransparency)
        {
            for (var i = 3; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
            }
        }

        var original = Image.FromBgra32(width, height, pixels);
        var decoded = Image.FromBytes(original.ToPng());

        await Assert.That(decoded.Width).IsEqualTo(width);
        await Assert.That(decoded.Height).IsEqualTo(height);
        await Assert.That(decoded.Pixels.SequenceEqual(pixels)).IsTrue();
    }

    [Test]
    public async Task Save_ToStream_WritesPng()
    {
        using var stream = new MemoryStream();
        TestAssets.TestBench.Crop(new Rectangle(0, 0, 300, 200)).Save(stream);

        stream.Position = 0;
        var decoded = Image.FromStream(stream);

        await Assert.That(decoded.Width).IsEqualTo(300);
        await Assert.That(decoded.Height).IsEqualTo(200);
        await Assert.That(decoded.Pixels.SequenceEqual(TestAssets.TestBench.Crop(new Rectangle(0, 0, 300, 200)).Pixels)).IsTrue();
    }

    [Test]
    public async Task FromBytes_WhenNotAnImage_Throws()
    {
        Assert.Throws<ArgumentException>(() => Image.FromBytes("this is not an image"u8));
    }

    [Test]
    public async Task Crop_ReturnsRegion()
    {
        // A 3x2 image whose blue channel holds the index of each pixel.
        var pixels = new byte[3 * 2 * 4];
        for (var i = 0; i < 6; i++)
        {
            pixels[i * 4] = (byte)i;
            pixels[(i * 4) + 3] = 255;
        }

        var cropped = Image.FromBgra32(3, 2, pixels).Crop(new Rectangle(1, 0, 3, 2));

        await Assert.That(cropped.Width).IsEqualTo(2);
        await Assert.That(cropped.Height).IsEqualTo(2);
        await Assert.That(new[] { cropped.Pixels[0], cropped.Pixels[4], cropped.Pixels[8], cropped.Pixels[12] }).IsEquivalentTo(new byte[] { 1, 2, 4, 5 }, CollectionOrdering.Matching);
    }

    [Test]
    [Arguments(-1, 0, 2, 2)]
    [Arguments(0, 0, 4, 2)]
    [Arguments(0, 0, 3, 3)]
    [Arguments(1, 1, 1, 2)]
    public async Task Crop_WhenRegionIsInvalid_Throws(int left, int top, int right, int bottom)
    {
        var image = Image.FromBgra32(3, 2, new byte[24]);
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Crop(new Rectangle(left, top, right, bottom)));
    }
}
