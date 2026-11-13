using Xunit;

namespace Askaiser.Marionette.Tests;

public class ImageTests
{
    [Fact]
    public void FromBgra32_CopiesPixels()
    {
        byte[] pixels = [1, 2, 3, 255, 4, 5, 6, 255];
        var image = Image.FromBgra32(2, 1, pixels);
        pixels[0] = 99;

        Assert.Equal(2, image.Width);
        Assert.Equal(1, image.Height);
        Assert.Equal(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }, image.Pixels.ToArray());
    }

    [Fact]
    public void FromRgba32_SwapsRedAndBlue()
    {
        var image = Image.FromRgba32(1, 1, [10, 20, 30, 255]);
        Assert.Equal(new byte[] { 30, 20, 10, 255 }, image.Pixels.ToArray());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void FromBgra32_WhenInvalidDimensions_Throws(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Image.FromBgra32(width, height, []));
    }

    [Fact]
    public void FromBgra32_WhenPixelCountDoesNotMatch_Throws()
    {
        Assert.Throws<ArgumentException>(() => Image.FromBgra32(2, 2, new byte[15]));
    }

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(17, 5, false)]
    [InlineData(64, 33, false)]
    [InlineData(17, 5, true)]
    [InlineData(200, 120, true)]
    public void Png_RoundTrip_PreservesPixels(int width, int height, bool withTransparency)
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

        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
        Assert.Equal(pixels, decoded.Pixels.ToArray());
    }

    [Fact]
    public void Save_ToStream_WritesPng()
    {
        using var stream = new MemoryStream();
        TestAssets.TestBench.Crop(new Rectangle(0, 0, 300, 200)).Save(stream);

        stream.Position = 0;
        var decoded = Image.FromStream(stream);

        Assert.Equal(300, decoded.Width);
        Assert.Equal(200, decoded.Height);
        Assert.Equal(TestAssets.TestBench.Crop(new Rectangle(0, 0, 300, 200)).Pixels.ToArray(), decoded.Pixels.ToArray());
    }

    [Fact]
    public void FromBytes_WhenNotAnImage_Throws()
    {
        Assert.Throws<ArgumentException>(() => Image.FromBytes("this is not an image"u8));
    }

    [Fact]
    public void Crop_ReturnsRegion()
    {
        // A 3x2 image whose blue channel holds the index of each pixel.
        var pixels = new byte[3 * 2 * 4];
        for (var i = 0; i < 6; i++)
        {
            pixels[i * 4] = (byte)i;
            pixels[(i * 4) + 3] = 255;
        }

        var cropped = Image.FromBgra32(3, 2, pixels).Crop(new Rectangle(1, 0, 3, 2));

        Assert.Equal(2, cropped.Width);
        Assert.Equal(2, cropped.Height);
        Assert.Equal(new byte[] { 1, 2, 4, 5 }, new[] { cropped.Pixels[0], cropped.Pixels[4], cropped.Pixels[8], cropped.Pixels[12] });
    }

    [Theory]
    [InlineData(-1, 0, 2, 2)]
    [InlineData(0, 0, 4, 2)]
    [InlineData(0, 0, 3, 3)]
    [InlineData(1, 1, 1, 2)]
    public void Crop_WhenRegionIsInvalid_Throws(int left, int top, int right, int bottom)
    {
        var image = Image.FromBgra32(3, 2, new byte[24]);
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Crop(new Rectangle(left, top, right, bottom)));
    }
}
