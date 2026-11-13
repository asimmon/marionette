namespace Askaiser.Marionette.Tests;

internal static class TestAssets
{
    private static readonly Lazy<Image> LazyGoogleNews = new(() => Load("google-news.png"));
    private static readonly Lazy<Image> LazyTestBench = new(() => Load("test-bench.png"));

    /// <summary>Gets a 1920x1080 screenshot of a news website, used by the tests of the first versions of the library.</summary>
    public static Image GoogleNews => LazyGoogleNews.Value;

    /// <summary>Gets a 1920x1080 screenshot of the test bench page, see test-bench.html.</summary>
    public static Image TestBench => LazyTestBench.Value;

    public static string GetPath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
    }

    private static Image Load(string fileName)
    {
        return Image.FromFile(GetPath(fileName));
    }
}
