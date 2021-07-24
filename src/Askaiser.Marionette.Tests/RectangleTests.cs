using System;
using Xunit;

namespace Askaiser.Marionette.Tests
{
    public class RectangleTests
    {
        [Theory]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 1, 0, 0)]
        [InlineData(0, 0, -1, 0)]
        [InlineData(0, 0, 0, -1)]
        public void WhenArgumentOutOfRangeException_Throws(int left, int top, int right, int bottom)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rectangle(left, top, right, bottom));
        }

        [Fact]
        public void WidthAndHeight_Work()
        {
            var rect = new Rectangle(50, 20, 70, 90);
            Assert.Equal(20, rect.Width);
            Assert.Equal(70, rect.Height);
        }

        [Fact]
        public void Deconstruct_Work()
        {
            var (left, top, right, bottom) = new Rectangle(50, 20, 70, 90);

            Assert.Equal(50, left);
            Assert.Equal(20, top);
            Assert.Equal(70, right);
            Assert.Equal(90, bottom);
        }

        [Theory]
        [InlineData(10, 10, 10, 10, 10, 10)]
        [InlineData(10, 10, 20, 20, 15, 15)]
        [InlineData(10, 10, 15, 16, 12, 13)]
        public void Center_Works(int left, int top, int right, int bottom, int expectedCenterX, int expectedCenterY)
        {
            var rect = new Rectangle(left, top, right, bottom);
            var (centerX, centerY) = rect.Center;

            Assert.Equal(expectedCenterX, centerX);
            Assert.Equal(expectedCenterY, centerY);
        }

        [Fact]
        public void Corners()
        {
            var rect = new Rectangle(75, 63, 478, 362);
            Assert.Equal(new Point(75, 63), rect.TopLeft);
            Assert.Equal(new Point(478, 63), rect.TopRight);
            Assert.Equal(new Point(75, 362), rect.BottomLeft);
            Assert.Equal(new Point(478, 362), rect.BottomRight);
        }

        [Fact]
        public void ConstructorWithPoints()
        {
            var r1 = new Rectangle(75, 63, 478, 362);
            var r2 = new Rectangle(r1.TopLeft, r1.BottomRight);
            Assert.Equal(r1, r2);
        }

        [Fact]
        public void FromLeft()
        {
            var rect = new Rectangle(100, 200, 300, 400);
            Assert.Equal(new Rectangle(100, 200, 150, 400), rect.FromLeft(50));
        }

        [Fact]
        public void FromRight()
        {
            var rect = new Rectangle(100, 200, 300, 400);
            Assert.Equal(new Rectangle(250, 200, 300, 400), rect.FromRight(50));
        }

        [Fact]
        public void FromTop()
        {
            var rect = new Rectangle(100, 200, 300, 400);
            Assert.Equal(new Rectangle(100, 200, 300, 250), rect.FromTop(50));
        }

        [Fact]
        public void FromBottom()
        {
            var rect = new Rectangle(100, 200, 300, 400);
            Assert.Equal(new Rectangle(100, 350, 300, 400), rect.FromBottom(50));
        }

        [Fact]
        public void FromTopLeft()
        {
            var rect = new Rectangle(1, 1, 100, 100);
            Assert.Equal(new Rectangle(1, 1, 50, 30), rect.FromTopLeft(49, 29));
        }

        [Fact]
        public void FromTopRight()
        {
            var rect = new Rectangle(1, 1, 100, 100);
            Assert.Equal(new Rectangle(50, 1, 100, 30), rect.FromTopRight(50, 29));
        }

        [Fact]
        public void FromBottomLeft()
        {
            var rect = new Rectangle(1, 1, 100, 100);
            Assert.Equal(new Rectangle(1, 70, 50, 100), rect.FromBottomLeft(49, 30));
        }

        [Fact]
        public void FromBottomRight()
        {
            var rect = new Rectangle(1, 1, 100, 100);
            Assert.Equal(new Rectangle(50, 70, 100, 100), rect.FromBottomRight(50, 30));
        }

        [Fact]
        public void FromCenter()
        {
            var rect = new Rectangle(0, 0, 100, 100);
            Assert.Equal(new Rectangle(20, 30, 80, 70), rect.FromCenter(60, 40));
        }

        [Fact]
        public void Multiply()
        {
            var rect = new Rectangle(5, 5, 20, 20);
            Assert.Equal(new Rectangle(10, 15, 40, 60), rect * (2, 3));
        }

        [Fact]
        public void Divide()
        {
            var rect = new Rectangle(10, 15, 40, 60);
            Assert.Equal(new Rectangle(5, 5, 20, 20), rect / (2, 3));
        }

        [Fact]
        public void BoundingCenters()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            Assert.Equal(new Point(10, 110), rect.CenterLeft);
            Assert.Equal(new Point(100, 110), rect.CenterRight);
            Assert.Equal(new Point(55, 20), rect.CenterTop);
            Assert.Equal(new Point(55, 200), rect.CenterBottom);
        }

        [Fact]
        public void Operators()
        {
            var rect = new Rectangle(1, 2, 3, 4);

            Assert.Equal(new Rectangle(2, 4, 4, 6), rect + (1, 2));
            Assert.Equal(new Rectangle(2, 4, 4, 6), rect + new Point(1, 2));

            Assert.Equal(new Rectangle(0, 0, 2, 2), rect - (1, 2));
            Assert.Equal(new Rectangle(0, 0, 2, 2), rect - new Point(1, 2));
        }
    }
}
