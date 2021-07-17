using Xunit;

namespace Askaiser.Marionette.Tests
{
    public class ImageLibraryAttributeTests
    {
        [Fact]
        public void EnsureImageLibraryAttributeFullNameUsedInSourceGeneratorIsCorrect()
        {
            // Whenever this fails, check the source generator to validate it also look for this type full name
            Assert.Equal("Askaiser.Marionette.ImageLibraryAttribute", typeof(ImageLibraryAttribute).FullName);
        }
    }
}
