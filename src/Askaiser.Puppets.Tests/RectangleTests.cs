using System;
using Xunit;

namespace Askaiser.Puppets.Tests
{
    public class RectangleTests
    {
        [Theory]
        [InlineData(-1, 0, 0, 0)]
        [InlineData(0, -1, 0, 0)]
        [InlineData(0, 0, -1, 0)]
        [InlineData(0, 0, 0, -1)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(0, 1, 0, 0)]
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
    }
}
