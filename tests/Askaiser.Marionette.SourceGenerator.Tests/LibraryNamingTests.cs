namespace Askaiser.Marionette.SourceGenerator.Tests;

public class LibraryNamingTests
{
    [Test]
    [Arguments("okbutton.png", "", "Okbutton", 0, "0.95", false)]
    [Arguments("ok-button.png", "", "OkButton", 0, "0.95", false)]
    [Arguments("OK-BUTTON.PNG", "", "OkButton", 0, "0.95", false)]
    [Arguments("my-feature/ok-button.png", "MyFeature", "OkButton", 0, "0.95", false)]
    [Arguments("my-feature--ok-button.png", "MyFeature", "OkButton", 0, "0.95", false)]
    [Arguments("pages/login--form--submit.jpg", "Pages.Login.Form", "Submit", 0, "0.95", false)]
    [Arguments("foo_gs.png", "", "Foo", 0, "0.95", true)]
    [Arguments("foo_GS.png", "", "Foo", 0, "0.95", true)]
    [Arguments("foo_0.8.png", "", "Foo", 0, "0.8", false)]
    [Arguments("foo_1.0.png", "", "Foo", 0, "1.0", false)]
    [Arguments("foo_0.1234.png", "", "Foo", 0, "0.1234", false)]
    [Arguments("ok-button_0.png", "", "OkButton", 0, "0.95", false)]
    [Arguments("ok-button_2.png", "", "OkButton", 2, "0.95", false)]
    [Arguments("header_gs_0.85_0.png", "", "Header", 0, "0.85", true)]
    [Arguments("header_gs_0.9_1.png", "", "Header", 1, "0.9", true)]
    [Arguments("header_12.bmp", "", "Header", 12, "0.95", false)]
    [Arguments("foo_unknown_gs.png", "", "Foo", 0, "0.95", true)]
    [Arguments("foo_0.12345.png", "", "Foo", 0, "0.95", false)]
    [Arguments("foo_2.0.png", "", "Foo", 0, "0.95", false)]
    [Arguments("foo_01.png", "", "Foo", 0, "0.95", false)]
    [Arguments("2fa-code.png", "", "_2faCode", 0, "0.95", false)]
    [Arguments("save (copy).png", "", "Savecopy", 0, "0.95", false)]
    [Arguments("_gs.png", "", "Gs", 0, "0.95", false)]
    public async Task Parse_ReadsNamesAndOptions(string relativePath, string expectedLibraries, string expectedName, int expectedIndex, string expectedThreshold, bool expectedGrayscale)
    {
        var parsed = LibraryNaming.Parse(relativePath);

        await Assert.That(parsed).IsNotNull();
        await Assert.That(string.Join(".", parsed!.Libraries)).IsEqualTo(expectedLibraries);
        await Assert.That(parsed.PropertyName).IsEqualTo(expectedName);
        await Assert.That(parsed.GroupIndex).IsEqualTo(expectedIndex);
        await Assert.That(parsed.Threshold).IsEqualTo(decimal.Parse(expectedThreshold, System.Globalization.CultureInfo.InvariantCulture));
        await Assert.That(parsed.Grayscale).IsEqualTo(expectedGrayscale);
    }

    [Test]
    [Arguments("_.png")]
    [Arguments("--.png")]
    [Arguments("---.png")]
    [Arguments("(!).png")]
    [Arguments("!!!/ok.png")]
    public async Task Parse_WhenNothingCanBeNamed_ReturnsNull(string relativePath)
    {
        await Assert.That(LibraryNaming.Parse(relativePath)).IsNull();
    }

    [Test]
    [Arguments("image.png", true)]
    [Arguments("image.PNG", true)]
    [Arguments("image.jpg", true)]
    [Arguments("image.jpeg", true)]
    [Arguments("image.bmp", true)]
    [Arguments("image.gif", false)]
    [Arguments("image.png.txt", false)]
    [Arguments("image", false)]
    public async Task IsImage_OnlyAcceptsSupportedFormats(string path, bool expected)
    {
        await Assert.That(LibraryNaming.IsImage(path)).IsEqualTo(expected);
    }
}
