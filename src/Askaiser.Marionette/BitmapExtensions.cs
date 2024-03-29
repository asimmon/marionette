﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Askaiser.Marionette;

internal static class BitmapExtensions
{
    public static Bitmap Crop(this Bitmap src, Rectangle newSize)
    {
        if (newSize == null)
        {
            throw new ArgumentNullException(nameof(newSize));
        }

        if (newSize.Left > src.Width)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), Messages.BitmapExtensions_Crop_Throw_LeftGreaterThanImageWidth.FormatInvariant(newSize.Left, src.Width));
        }

        if (newSize.Right > src.Width)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), Messages.BitmapExtensions_Crop_Throw_RightGreaterThanImageWidth.FormatInvariant(newSize.Right, src.Width));
        }

        if (newSize.Top > src.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), Messages.BitmapExtensions_Crop_Throw_TopGreaterThanImageHeight.FormatInvariant(newSize.Top, src.Height));
        }

        if (newSize.Bottom > src.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), Messages.BitmapExtensions_Crop_Throw_BottomGreaterThanImageHeight.FormatInvariant(newSize.Bottom, src.Height));
        }

        if (newSize.Width == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), Messages.BitmapExtensions_Crop_Throw_NewWidthZero);
        }

        if (newSize.Height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), Messages.BitmapExtensions_Crop_Throw_NewHeightZero);
        }

        var dst = new Bitmap(newSize.Width, newSize.Height);

        var dstRect = new System.Drawing.Rectangle(0, 0, dst.Width, dst.Height);
        var srcRect = new System.Drawing.Rectangle(newSize.Left, newSize.Top, newSize.Width, newSize.Height);

        using (var graphics = Graphics.FromImage(dst))
        {
            graphics.DrawImage(src, dstRect, srcRect, GraphicsUnit.Pixel);
        }

        return dst;
    }

    public static byte[] GetBytes(this Image image, ImageFormat format)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        using var stream = new MemoryStream();
        image.Save(stream, format);
        return stream.ToArray();
    }
}
