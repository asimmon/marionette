using System;

namespace Askaiser.UITesting
{
    internal static class RectangleExtensions
    {
        public static Point GetCenter(this Rectangle rect)
        {
            var halfWidth = (int)Math.Round(rect.Width / 2d);
            var halfHeight = (int)Math.Round(rect.Height / 2d);
            return new Point(rect.Left + halfWidth, rect.Top + halfHeight);
        }
    }
}