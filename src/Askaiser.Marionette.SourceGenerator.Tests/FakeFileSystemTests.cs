using Xunit;

namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public class FakeFileSystemTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FakeFileSystem_Works(bool useAbsolutePath)
        {
            var fs = new FakeFileSystem();
            var prefix = useAbsolutePath ? "C:\\" : string.Empty;

            fs.SetFileBytes(prefix + "src/readme.md", new byte[] { 0 });
            fs.SetFileBytes(prefix + "src/images/logo.png", new byte[] { 1, 2 });
            fs.SetFileBytes(prefix + "src/images/header.jpg", new byte[] { 1, 2, 3 });
            fs.SetFileBytes(prefix + "src/fonts/arial.ttf", new byte[] { 1, 2, 3, 4 });
            fs.AddEntry(prefix + "src/images/sidebar");

            Assert.Equal(new[] { prefix + "src\\images", prefix + "src\\fonts" }, fs.EnumerateDirectories(prefix + "src"));
            Assert.Equal(new[] { prefix + "src\\images\\sidebar" }, fs.EnumerateDirectories(prefix + "src\\images"));

            Assert.Equal(new[] { prefix + "src\\readme.md" }, fs.EnumerateFiles(prefix + "src"));
            Assert.Equal(new[] { prefix + "src\\images\\logo.png", prefix + "src\\images\\header.jpg" }, fs.EnumerateFiles(prefix + "src/images"));
            Assert.Equal(new[] { prefix + "src\\fonts\\arial.ttf" }, fs.EnumerateFiles(prefix + "src\\fonts"));

            Assert.Equal(1, fs.GetFileSize(prefix + "src/readme.md"));
            Assert.Equal(2, fs.GetFileSize(prefix + "src\\images\\logo.png"));
            Assert.Equal(new byte[] { 1, 2, 3 }, fs.GetFileBytes(prefix + "src\\images\\header.jpg"));
        }
    }
}
