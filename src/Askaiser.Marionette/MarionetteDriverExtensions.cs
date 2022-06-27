using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

public static class MarionetteDriverExtensions
{
    #region Mouse interaction with the first available element of a collection

    public static async Task MoveToAny(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAny(elements, waitFor, searchRect).ConfigureAwait(false);
        await MoveTo(driver, result).ConfigureAwait(false);
    }

    public static async Task SingleClickAny(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAny(elements, waitFor, searchRect).ConfigureAwait(false);
        await SingleClick(driver, result).ConfigureAwait(false);
    }

    public static async Task DoubleClickAny(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAny(elements, waitFor, searchRect).ConfigureAwait(false);
        await DoubleClick(driver, result).ConfigureAwait(false);
    }

    public static async Task TripleClickAny(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAny(elements, waitFor, searchRect).ConfigureAwait(false);
        await TripleClick(driver, result).ConfigureAwait(false);
    }

    public static async Task RightClickAny(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAny(elements, waitFor, searchRect).ConfigureAwait(false);
        await RightClick(driver, result).ConfigureAwait(false);
    }

    public static async Task DragFromAny(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAny(elements, waitFor, searchRect).ConfigureAwait(false);
        await DragFrom(driver, result).ConfigureAwait(false);
    }

    public static async Task DropToAny(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAny(elements, waitFor, searchRect).ConfigureAwait(false);
        await DropTo(driver, result).ConfigureAwait(false);
    }

    #endregion

    #region Mouse interaction with points

    public static async Task MoveTo(this MarionetteDriver driver, Point coordinates)
    {
        var (x, y) = coordinates;
        await driver.MoveTo(x, y).ConfigureAwait(false);
    }

    public static async Task SingleClick(this MarionetteDriver driver, Point coordinates)
    {
        var (x, y) = coordinates;
        await driver.SingleClick(x, y).ConfigureAwait(false);
    }

    public static async Task DoubleClick(this MarionetteDriver driver, Point coordinates)
    {
        var (x, y) = coordinates;
        await driver.DoubleClick(x, y).ConfigureAwait(false);
    }

    public static async Task TripleClick(this MarionetteDriver driver, Point coordinates)
    {
        var (x, y) = coordinates;
        await driver.TripleClick(x, y).ConfigureAwait(false);
    }

    public static async Task RightClick(this MarionetteDriver driver, Point coordinates)
    {
        var (x, y) = coordinates;
        await driver.RightClick(x, y).ConfigureAwait(false);
    }

    public static async Task DragFrom(this MarionetteDriver driver, Point coordinates)
    {
        var (x, y) = coordinates;
        await driver.DragFrom(x, y).ConfigureAwait(false);
    }

    public static async Task DropTo(this MarionetteDriver driver, Point coordinates)
    {
        var (x, y) = coordinates;
        await driver.DropTo(x, y).ConfigureAwait(false);
    }

    #endregion

    #region Mouse interaction with search result

    public static async Task MoveTo(this MarionetteDriver driver, SearchResult searchResult) => await MoveTo(driver, searchResult.Location.Center).ConfigureAwait(false);

    public static async Task SingleClick(this MarionetteDriver driver, SearchResult searchResult) => await SingleClick(driver, searchResult.Location.Center).ConfigureAwait(false);

    public static async Task DoubleClick(this MarionetteDriver driver, SearchResult searchResult) => await DoubleClick(driver, searchResult.Location.Center).ConfigureAwait(false);

    public static async Task TripleClick(this MarionetteDriver driver, SearchResult searchResult) => await TripleClick(driver, searchResult.Location.Center).ConfigureAwait(false);

    public static async Task RightClick(this MarionetteDriver driver, SearchResult searchResult) => await RightClick(driver, searchResult.Location.Center).ConfigureAwait(false);

    public static async Task DragFrom(this MarionetteDriver driver, SearchResult searchResult) => await DragFrom(driver, searchResult.Location.Center).ConfigureAwait(false);

    public static async Task DropTo(this MarionetteDriver driver, SearchResult searchResult) => await DropTo(driver, searchResult.Location.Center).ConfigureAwait(false);

    #endregion

    #region Mouse interaction with an element

    public static async Task MoveTo(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
        await MoveTo(driver, result).ConfigureAwait(false);
    }

    public static async Task SingleClick(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
        await SingleClick(driver, result).ConfigureAwait(false);
    }

    public static async Task DoubleClick(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
        await DoubleClick(driver, result).ConfigureAwait(false);
    }

    public static async Task TripleClick(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
        await TripleClick(driver, result).ConfigureAwait(false);
    }

    public static async Task RightClick(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
        await RightClick(driver, result).ConfigureAwait(false);
    }

    public static async Task DragFrom(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
        await DragFrom(driver, result).ConfigureAwait(false);
    }

    public static async Task DropTo(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect).ConfigureAwait(false);
        await DropTo(driver, result).ConfigureAwait(false);
    }

    #endregion

    #region Element visibility

    public static async Task<bool> IsVisible(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect, NoSingleResultBehavior.Ignore).ConfigureAwait(false);
        return result.Success;
    }

    public static async Task<bool> IsAnyVisible(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAny(elements, waitFor, searchRect, NoSingleResultBehavior.Ignore).ConfigureAwait(false);
        return result.Success;
    }

    public static async Task<bool> AreAllVisible(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitForAll(elements, waitFor, searchRect, NoSingleResultBehavior.Ignore).ConfigureAwait(false);
        return result.All(x => x.Success);
    }

    #endregion

    #region Key press with single key code

    public static async Task KeyPress(this MarionetteDriver driver, VirtualKeyCode keyCode, TimeSpan? sleepAfter = default)
    {
        await driver.KeyPress(new[] { keyCode }, sleepAfter).ConfigureAwait(false);
    }

    public static async Task KeyDown(this MarionetteDriver driver, VirtualKeyCode keyCode, TimeSpan? sleepAfter = default)
    {
        await driver.KeyDown(new[] { keyCode }, sleepAfter).ConfigureAwait(false);
    }

    public static async Task KeyUp(this MarionetteDriver driver, VirtualKeyCode keyCode, TimeSpan? sleepAfter = default)
    {
        await driver.KeyUp(new[] { keyCode }, sleepAfter).ConfigureAwait(false);
    }

    #endregion

    #region Text-based actions

    public static async Task<SearchResult> WaitFor(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        return await driver.WaitFor(new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task MoveTo(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        await MoveTo(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task SingleClick(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        await SingleClick(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task DoubleClick(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        await DoubleClick(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task TripleClick(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        await TripleClick(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task RightClick(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        await RightClick(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task DragFrom(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        await DragFrom(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task DropTo(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        await DropTo(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task<bool> IsVisible(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        return await IsVisible(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    #endregion

    #region System.Drawing.Image-based actions

    public static async Task<SearchResult> WaitFor(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        return await driver.WaitFor(new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task MoveTo(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        await MoveTo(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task SingleClick(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        await SingleClick(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task DoubleClick(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        await DoubleClick(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task TripleClick(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        await TripleClick(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task RightClick(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        await RightClick(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task DragFrom(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        await DragFrom(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task DropTo(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        await DropTo(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task<bool> IsVisible(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        return await IsVisible(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    #endregion

    #region Finding elements locations without throwing exceptions

    public static async Task<Rectangle[]> FindLocations(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default)
    {
        var result = await driver.WaitFor(element, waitFor, searchRect, NoSingleResultBehavior.Ignore).ConfigureAwait(false);
        return result.Locations.ToArray();
    }

    public static async Task<Rectangle[]> FindLocations(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.BlackAndWhite)
    {
        return await FindLocations(driver, new TextElement(text, textOptions), waitFor, searchRect).ConfigureAwait(false);
    }

    public static async Task<Rectangle[]> FindLocations(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false)
    {
        return await FindLocations(driver, new ImageElement("image", image.GetBytes(ImageFormat.Png), threshold, grayscale), waitFor, searchRect).ConfigureAwait(false);
    }

    #endregion
}
