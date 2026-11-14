using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette;

/// <summary>
/// Shortcuts that combine a search with a mouse or keyboard action.
/// </summary>
/// <remarks>
/// <para>
/// Methods that take an element, a text or an image first wait for it to be visible on the current monitor, like
/// <see cref="MarionetteDriver.WaitForAsync(IElement, TimeSpan?, Rectangle?, CancellationToken)"/> does, then act on its center.
/// They throw an <see cref="ElementNotFoundException"/> when it is not visible and a <see cref="MultipleElementFoundException"/>
/// when it is visible at several locations, in which case the mouse is not touched.
/// </para>
/// <para>
/// Every method accepts a <see cref="CancellationToken"/> and throws an <see cref="OperationCanceledException"/> when it is canceled.
/// </para>
/// </remarks>
public static class MarionetteDriverExtensions
{
    private const string ImageElementName = "image";

    #region Mouse interaction with the first available element of a collection

    /// <summary>
    /// Waits for any of the elements to be visible, then moves the mouse cursor to the center of the first visible one.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="elements">The images or texts to look for, by order of preference.</param>
    /// <param name="waitFor">How long to wait for the elements. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ElementNotFoundException">None of the elements is visible.</exception>
    /// <exception cref="MultipleElementFoundException">The first visible element is visible at several locations.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async Task MoveToAnyAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAnyAsync(elements, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await MoveToAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any of the elements to be visible, then clicks the center of the first visible one with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task SingleClickAnyAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAnyAsync(elements, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await SingleClickAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any of the elements to be visible, then double-clicks the center of the first visible one with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task DoubleClickAnyAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAnyAsync(elements, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await DoubleClickAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any of the elements to be visible, then triple-clicks the center of the first visible one with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task TripleClickAnyAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAnyAsync(elements, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await TripleClickAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any of the elements to be visible, then clicks the center of the first visible one with the right mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task RightClickAnyAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAnyAsync(elements, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await RightClickAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any of the elements to be visible, then presses the left mouse button on the center of the first visible one, without releasing it.
    /// </summary>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task DragFromAnyAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAnyAsync(elements, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await DragFromAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for any of the elements to be visible, then moves the mouse cursor to the center of the first visible one and releases the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAnyAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task DropToAnyAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAnyAsync(elements, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await DropToAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Mouse interaction with points

    /// <summary>
    /// Moves the mouse cursor to a position.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="coordinates">The position, in pixels of the virtual desktop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task MoveToAsync(this MarionetteDriver driver, Point coordinates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        return driver.MoveToAsync(coordinates.X, coordinates.Y, cancellationToken);
    }

    /// <summary>
    /// Clicks a position with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Point, CancellationToken)" path="/param"/>
    public static Task SingleClickAsync(this MarionetteDriver driver, Point coordinates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        return driver.SingleClickAsync(coordinates.X, coordinates.Y, cancellationToken);
    }

    /// <summary>
    /// Double-clicks a position with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Point, CancellationToken)" path="/param"/>
    public static Task DoubleClickAsync(this MarionetteDriver driver, Point coordinates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        return driver.DoubleClickAsync(coordinates.X, coordinates.Y, cancellationToken);
    }

    /// <summary>
    /// Triple-clicks a position with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Point, CancellationToken)" path="/param"/>
    public static Task TripleClickAsync(this MarionetteDriver driver, Point coordinates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        return driver.TripleClickAsync(coordinates.X, coordinates.Y, cancellationToken);
    }

    /// <summary>
    /// Clicks a position with the right mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Point, CancellationToken)" path="/param"/>
    public static Task RightClickAsync(this MarionetteDriver driver, Point coordinates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        return driver.RightClickAsync(coordinates.X, coordinates.Y, cancellationToken);
    }

    /// <summary>
    /// Presses the left mouse button on a position, without releasing it.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Point, CancellationToken)" path="/param"/>
    public static Task DragFromAsync(this MarionetteDriver driver, Point coordinates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        return driver.DragFromAsync(coordinates.X, coordinates.Y, cancellationToken);
    }

    /// <summary>
    /// Moves the mouse cursor to a position and releases the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Point, CancellationToken)" path="/param"/>
    public static Task DropToAsync(this MarionetteDriver driver, Point coordinates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        return driver.DropToAsync(coordinates.X, coordinates.Y, cancellationToken);
    }

    #endregion

    #region Mouse interaction with search result

    /// <summary>
    /// Moves the mouse cursor to the center of an element that was found.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="searchResult">The result of a search, which must have a single location.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ElementNotFoundException">The result has no location.</exception>
    /// <exception cref="MultipleElementFoundException">The result has several locations.</exception>
    public static Task MoveToAsync(this MarionetteDriver driver, SearchResult searchResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchResult);
        return MoveToAsync(driver, searchResult.Location.Center, cancellationToken);
    }

    /// <summary>
    /// Clicks the center of an element that was found with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/exception"/>
    public static Task SingleClickAsync(this MarionetteDriver driver, SearchResult searchResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchResult);
        return SingleClickAsync(driver, searchResult.Location.Center, cancellationToken);
    }

    /// <summary>
    /// Double-clicks the center of an element that was found with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/exception"/>
    public static Task DoubleClickAsync(this MarionetteDriver driver, SearchResult searchResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchResult);
        return DoubleClickAsync(driver, searchResult.Location.Center, cancellationToken);
    }

    /// <summary>
    /// Triple-clicks the center of an element that was found with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/exception"/>
    public static Task TripleClickAsync(this MarionetteDriver driver, SearchResult searchResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchResult);
        return TripleClickAsync(driver, searchResult.Location.Center, cancellationToken);
    }

    /// <summary>
    /// Clicks the center of an element that was found with the right mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/exception"/>
    public static Task RightClickAsync(this MarionetteDriver driver, SearchResult searchResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchResult);
        return RightClickAsync(driver, searchResult.Location.Center, cancellationToken);
    }

    /// <summary>
    /// Presses the left mouse button on the center of an element that was found, without releasing it.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/exception"/>
    public static Task DragFromAsync(this MarionetteDriver driver, SearchResult searchResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchResult);
        return DragFromAsync(driver, searchResult.Location.Center, cancellationToken);
    }

    /// <summary>
    /// Moves the mouse cursor to the center of an element that was found and releases the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, SearchResult, CancellationToken)" path="/exception"/>
    public static Task DropToAsync(this MarionetteDriver driver, SearchResult searchResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchResult);
        return DropToAsync(driver, searchResult.Location.Center, cancellationToken);
    }

    #endregion

    #region Mouse interaction with an element

    /// <summary>
    /// Waits for an element to be visible, then moves the mouse cursor to its center.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="element">The image or text to look for.</param>
    /// <param name="waitFor">How long to wait for the element. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ElementNotFoundException">The element is not visible.</exception>
    /// <exception cref="MultipleElementFoundException">The element is visible at several locations.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async Task MoveToAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await MoveToAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for an element to be visible, then clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task SingleClickAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await SingleClickAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for an element to be visible, then double-clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task DoubleClickAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await DoubleClickAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for an element to be visible, then triple-clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task TripleClickAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await TripleClickAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for an element to be visible, then clicks its center with the right mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task RightClickAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await RightClickAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for an element to be visible, then presses the left mouse button on its center, without releasing it.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task DragFromAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await DragFromAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for an element to be visible, then moves the mouse cursor to its center and releases the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task DropToAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, cancellationToken).ConfigureAwait(false);
        await DropToAsync(driver, result, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Element visibility

    /// <summary>
    /// Returns whether an element is visible on the current monitor, at one location or more. This method does not throw when it is not.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="element">The image or text to look for.</param>
    /// <param name="waitFor">How long to wait for the element. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async Task<bool> IsVisibleAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, NoSingleResultBehavior.Ignore, cancellationToken).ConfigureAwait(false);
        return result.Success;
    }

    /// <summary>
    /// Returns whether at least one of the elements is visible on the current monitor. This method does not throw when none is.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="elements">The images or texts to look for.</param>
    /// <param name="waitFor">How long to wait for the elements. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async Task<bool> IsAnyVisibleAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAnyAsync(elements, waitFor, searchRect, NoSingleResultBehavior.Ignore, cancellationToken).ConfigureAwait(false);
        return result.Success;
    }

    /// <summary>
    /// Returns whether all the elements are visible on the current monitor. This method does not throw when some are not.
    /// </summary>
    /// <inheritdoc cref="IsAnyVisibleAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="IsAnyVisibleAsync(MarionetteDriver, IEnumerable{IElement}, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task<bool> AreAllVisibleAsync(this MarionetteDriver driver, IEnumerable<IElement> elements, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var results = await driver.WaitForAllAsync(elements, waitFor, searchRect, NoSingleResultBehavior.Ignore, cancellationToken).ConfigureAwait(false);

        foreach (var result in results)
        {
            if (!result.Success)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Key press with single key code

    /// <summary>
    /// Presses and releases a key.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="keyCode">The key.</param>
    /// <param name="sleepAfter">How long to pause afterwards. When null, the default duration of the driver is used.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task KeyPressAsync(this MarionetteDriver driver, VirtualKeyCode keyCode, TimeSpan? sleepAfter = default, CancellationToken cancellationToken = default)
    {
        return driver.KeyPressAsync([keyCode], sleepAfter, cancellationToken);
    }

    /// <summary>
    /// Presses a key without releasing it. Use <see cref="KeyUpAsync(MarionetteDriver, VirtualKeyCode, TimeSpan?, CancellationToken)"/> to release it.
    /// </summary>
    /// <inheritdoc cref="KeyPressAsync(MarionetteDriver, VirtualKeyCode, TimeSpan?, CancellationToken)" path="/param"/>
    public static Task KeyDownAsync(this MarionetteDriver driver, VirtualKeyCode keyCode, TimeSpan? sleepAfter = default, CancellationToken cancellationToken = default)
    {
        return driver.KeyDownAsync([keyCode], sleepAfter, cancellationToken);
    }

    /// <summary>
    /// Releases a key.
    /// </summary>
    /// <inheritdoc cref="KeyPressAsync(MarionetteDriver, VirtualKeyCode, TimeSpan?, CancellationToken)" path="/param"/>
    public static Task KeyUpAsync(this MarionetteDriver driver, VirtualKeyCode keyCode, TimeSpan? sleepAfter = default, CancellationToken cancellationToken = default)
    {
        return driver.KeyUpAsync([keyCode], sleepAfter, cancellationToken);
    }

    #endregion

    #region Text-based actions

    /// <summary>
    /// Waits for a text to be visible on the current monitor and returns where it is.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/exception"/>
    public static Task<SearchResult> WaitForAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return driver.WaitForAsync(new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for a text to be visible, then moves the mouse cursor to its center.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="text">The text to look for. Its case is ignored.</param>
    /// <param name="waitFor">How long to wait for the text. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <param name="textOptions">The preprocessing applied to the screen before reading it. The default, none, is the most accurate in most cases.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ElementNotFoundException">The text is not visible.</exception>
    /// <exception cref="MultipleElementFoundException">The text is visible at several locations.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static Task MoveToAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return MoveToAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for a text to be visible, then clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/exception"/>
    public static Task SingleClickAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return SingleClickAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for a text to be visible, then double-clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/exception"/>
    public static Task DoubleClickAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return DoubleClickAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for a text to be visible, then triple-clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/exception"/>
    public static Task TripleClickAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return TripleClickAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for a text to be visible, then clicks its center with the right mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/exception"/>
    public static Task RightClickAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return RightClickAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for a text to be visible, then presses the left mouse button on its center, without releasing it.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/exception"/>
    public static Task DragFromAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return DragFromAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for a text to be visible, then moves the mouse cursor to its center and releases the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/exception"/>
    public static Task DropToAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return DropToAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Returns whether a text is visible on the current monitor, at one location or more. This method does not throw when it is not.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static Task<bool> IsVisibleAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return IsVisibleAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    #endregion

    #region Image-based actions

    /// <summary>
    /// Waits for an image to be visible on the current monitor and returns where it is.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/exception"/>
    public static Task<SearchResult> WaitForAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return driver.WaitForAsync(new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for an image to be visible, then moves the mouse cursor to its center.
    /// </summary>
    /// <param name="driver">The driver.</param>
    /// <param name="image">The image to look for.</param>
    /// <param name="waitFor">How long to wait for the image. When null, the default duration of the driver is used.</param>
    /// <param name="searchRect">The part of the screen to search, or null to search the whole monitor. Searching a small area is much faster.</param>
    /// <param name="threshold">How similar a region of the screen must be to the image to be considered a match, from 0 to 1.</param>
    /// <param name="grayscale">Whether colors are ignored when comparing the image with the screen.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ElementNotFoundException">The image is not visible.</exception>
    /// <exception cref="MultipleElementFoundException">The image is visible at several locations.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static Task MoveToAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return MoveToAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for an image to be visible, then clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/exception"/>
    public static Task SingleClickAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return SingleClickAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for an image to be visible, then double-clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/exception"/>
    public static Task DoubleClickAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return DoubleClickAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for an image to be visible, then triple-clicks its center with the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/exception"/>
    public static Task TripleClickAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return TripleClickAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for an image to be visible, then clicks its center with the right mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/exception"/>
    public static Task RightClickAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return RightClickAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for an image to be visible, then presses the left mouse button on its center, without releasing it.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/exception"/>
    public static Task DragFromAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return DragFromAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Waits for an image to be visible, then moves the mouse cursor to its center and releases the left mouse button.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/exception"/>
    public static Task DropToAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return DropToAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Returns whether an image is visible on the current monitor, at one location or more. This method does not throw when it is not.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static Task<bool> IsVisibleAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return IsVisibleAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    #endregion

    #region Finding elements locations without throwing exceptions

    /// <summary>
    /// Returns every location of an element on the current monitor, in pixels of the virtual desktop. The result is empty when the element is not visible.
    /// </summary>
    /// <inheritdoc cref="IsVisibleAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/param"/>
    /// <inheritdoc cref="IsVisibleAsync(MarionetteDriver, IElement, TimeSpan?, Rectangle?, CancellationToken)" path="/exception"/>
    public static async Task<Rectangle[]> FindLocationsAsync(this MarionetteDriver driver, IElement element, TimeSpan? waitFor = default, Rectangle? searchRect = default, CancellationToken cancellationToken = default)
    {
        var result = await driver.WaitForAsync(element, waitFor, searchRect, NoSingleResultBehavior.Ignore, cancellationToken).ConfigureAwait(false);
        return result.Locations.ToArray();
    }

    /// <summary>
    /// Returns every location of a text on the current monitor, in pixels of the virtual desktop. The result is empty when the text is not visible.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, string, TimeSpan?, Rectangle?, TextOptions, CancellationToken)" path="/param"/>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static Task<Rectangle[]> FindLocationsAsync(this MarionetteDriver driver, string text, TimeSpan? waitFor = default, Rectangle? searchRect = default, TextOptions textOptions = TextOptions.None, CancellationToken cancellationToken = default)
    {
        return FindLocationsAsync(driver, new TextElement(text, textOptions), waitFor, searchRect, cancellationToken);
    }

    /// <summary>
    /// Returns every location of an image on the current monitor, in pixels of the virtual desktop. The result is empty when the image is not visible.
    /// </summary>
    /// <inheritdoc cref="MoveToAsync(MarionetteDriver, Image, TimeSpan?, Rectangle?, decimal, bool, CancellationToken)" path="/param"/>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static Task<Rectangle[]> FindLocationsAsync(this MarionetteDriver driver, Image image, TimeSpan? waitFor = default, Rectangle? searchRect = default, decimal threshold = ImageElement.DefaultThreshold, bool grayscale = false, CancellationToken cancellationToken = default)
    {
        return FindLocationsAsync(driver, new ImageElement(ImageElementName, image, threshold, grayscale), waitFor, searchRect, cancellationToken);
    }

    #endregion
}
