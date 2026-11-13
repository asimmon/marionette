using TUnit.Assertions.Enums;
using Askaiser.Marionette.Recognition.Text;

namespace Askaiser.Marionette.Tests;

public class TextFinderTests
{
    [Test]
    public async Task Find_ReturnsBoundsOfTheSearchedWordsOnly()
    {
        var rows = new[] { CreateLine("File Edit View", left: 100, top: 20, characterWidth: 10, height: 16) };

        var location = await Assert.That(TextFinder.Find(rows, "Edit", ignoreCase: true)).HasSingleItem();

        // "Edit" starts at the sixth character.
        await Assert.That(location).IsEqualTo(new Rectangle(150, 20, 190, 36));
    }

    [Test]
    public async Task Find_ReturnsEveryOccurrence()
    {
        var rows = new[]
        {
            CreateLine("one two one", left: 0, top: 0, characterWidth: 10, height: 10),
            CreateLine("none", left: 0, top: 50, characterWidth: 10, height: 10),
        };

        var locations = TextFinder.Find(rows, "one", ignoreCase: true);

        await Assert.That(locations).IsEquivalentTo([new Rectangle(0, 0, 30, 10), new Rectangle(80, 0, 110, 10), new Rectangle(10, 50, 40, 60)], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments("save changes", true, true)]
    [Arguments("save changes", false, false)]
    [Arguments("Save changes", false, true)]
    [Arguments("SAVE CHANGES", true, true)]
    public async Task Find_HonorsCase(string searched, bool ignoreCase, bool expectedFound)
    {
        var rows = new[] { CreateLine("Save changes", left: 0, top: 0, characterWidth: 8, height: 12) };
        await Assert.That(TextFinder.Find(rows, searched, ignoreCase).Count == 1).IsEqualTo(expectedFound);
    }

    [Test]
    [Arguments("  Save   changes ")]
    [Arguments("Save\tchanges")]
    [Arguments("Save\r\nchanges")]
    public async Task Find_NormalizesWhitespace(string searched)
    {
        var rows = new[] { CreateLine("Save  changes", left: 0, top: 0, characterWidth: 8, height: 12) };

        var location = await Assert.That(TextFinder.Find(rows, searched, ignoreCase: true)).HasSingleItem();
        await Assert.That(location).IsEqualTo(new Rectangle(0, 0, 104, 12));
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("missing")]
    public async Task Find_WhenTextIsAbsentOrBlank_ReturnsNothing(string searched)
    {
        var rows = new[] { CreateLine("Save changes", left: 0, top: 0, characterWidth: 8, height: 12) };
        await Assert.That(TextFinder.Find(rows, searched, ignoreCase: true)).IsEmpty();
    }

    [Test]
    public async Task JoinRows_JoinsNeighborsOfTheSameRowFromLeftToRight()
    {
        var rows = TextFinder.JoinRows(
        [
            CreateLine("Edit", left: 60, top: 21, characterWidth: 10, height: 16),
            CreateLine("File", left: 0, top: 20, characterWidth: 10, height: 16),
            null,
            CreateLine("Below", left: 0, top: 60, characterWidth: 10, height: 16),
        ]);

        await Assert.That(rows.Select(row => row.Text)).IsEquivalentTo(["File Edit", "Below"], CollectionOrdering.Matching);

        var location = await Assert.That(TextFinder.Find(rows, "File Edit", ignoreCase: false)).HasSingleItem();
        await Assert.That(location).IsEqualTo(new Rectangle(0, 20, 100, 37));
    }

    [Test]
    public async Task JoinRows_DoesNotJoinDistantText()
    {
        var rows = TextFinder.JoinRows(
        [
            CreateLine("Left", left: 0, top: 20, characterWidth: 10, height: 16),
            CreateLine("Right", left: 400, top: 20, characterWidth: 10, height: 16),
        ]);

        await Assert.That(rows.Count).IsEqualTo(2);
    }

    [Test]
    public async Task JoinRows_WhenBoxesOverlap_DoesNotRepeatCharacters()
    {
        // The end of the first piece was also read as the beginning of the second one.
        var rows = TextFinder.JoinRows(
        [
            CreateLine("var x = M", left: 0, top: 0, characterWidth: 10, height: 16),
            CreateLine("Math.Abs", left: 80, top: 0, characterWidth: 10, height: 16),
        ]);

        await Assert.That(rows.Select(row => row.Text)).IsEquivalentTo(["var x = Math.Abs"], CollectionOrdering.Matching);
    }

    private static TextLine CreateLine(string text, int left, int top, int characterWidth, int height)
    {
        var lefts = new float[text.Length];
        var rights = new float[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            lefts[i] = left + (i * characterWidth);
            rights[i] = left + ((i + 1) * characterWidth);
        }

        return new TextLine(text, lefts, rights, top, top + height, confidence: 1f);
    }
}
