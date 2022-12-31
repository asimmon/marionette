<!-- omit in toc -->
# Askaiser.Marionette

[![nuget](https://img.shields.io/nuget/v/Askaiser.Marionette.svg?logo=nuget)](https://www.nuget.org/packages/Askaiser.Marionette/)
[![build](https://img.shields.io/github/actions/workflow/status/asimmon/askaiser-marionette/ci.yml?branch=master&logo=github)](https://github.com/asimmon/askaiser-marionette/actions/workflows/ci.yml)

Askaiser.Marionette is a **test automation framework based on image and text recognition**. It includes a C# source generator that allows you to quickly interact with C# properties generated from images in your project or elsewhere. The framework is built on top of **OpenCV** and **Tesseract OCR** and only works on Windows for now.

**Table of contents**

- [🔥 Motivation and features](#-motivation-and-features)
- [🎥 Demo video of Windows Calculator automation](#-demo-video-of-windows-calculator-automation)
- [🚀 Getting started](#-getting-started)
- [🏭 Change the C# source generator behavior](#-change-the-c-source-generator-behavior)
- [✍ Manually creating image and text elements](#-manually-creating-image-and-text-elements)
- [📕 API reference](#-api-reference)
- [🤖 Running tests in a CI environment](#-running-tests-in-a-ci-environment)

## 🔥 Motivation and features

* Unlike other test automation frameworks, Askaiser.Marionette **does not rely on hardcoded identifiers, CSS or XPath selectors**. It uses image and text recognition to ensure that you interact with elements that are **actually visible** on the screen.
* Maintaining identifiers, CSS and XPath selectors over time can be hard. Capturing small screenshots and finding text with an OCR is not.
* With the built-in C# source generator, you can start **writing the test code right away**.
* You can interact with the whole operating system, instead of a single application.
* You can interact with any kind of desktop applications.
* It works well with [BDD](https://en.wikipedia.org/wiki/Behavior-driven_development) and [SpecFlow](https://specflow.org/).


## 🎥 Demo video of Windows Calculator automation

> This short video (1m40s) shows how to interact with an existing application with no prior knowledge of its source code, in a matter of minutes.

* `00:00` : Quickly capture screenshots of the app you're testing,
* `00:08` : Rename and organize your screenshots in a meaningful way,
* `00:22` : Drop your screenshots in your C# project,
* `00:30` : Use `ImageLibraryAttribute` to **automatically generate C# properties** from your screenshots,
* `01:06` : Use `MarionetteDriver` to interact with the generated properties (or text recognized by the OCR)!

https://user-images.githubusercontent.com/14242083/126416123-aebd0fce-825f-4ece-90e9-762503dc4cab.mp4


## 🚀 Getting started

```
dotnet add package Askaiser.Marionette
```

It supports **.NET Standard 2.0**, **.NET Standard 2.1** an **.NET 6**, but only on Windows for now.

```csharp
[ImageLibrary("path/to/your/images")]
public partial class MyLibrary
{
    // In the "path/to/your/images" directory, we assume here that there are multiple small screenshots of UI elements
    // with these relative paths: "pages/login/title.png", "pages/login/email.png", "pages/login/password.png" and "pages/login/submit.png".
    // These file names actually control the behavior of the C# source generator.
    // This behavior is explained in the next section.
}

using (var driver = MarionetteDriver.Create(/* optional DriverOptions */))
{
    // in this exemple, we enter a username and password in a login page
    await driver.WaitForAsync(MyLibrary.Instance.Pages.Login.Title, waitFor: TimeSpan.FromSeconds(5));

    await driver.SingleClickAsync(MyLibrary.Instance.Pages.Login.Email);
    await driver.TypeTextAsync("much@automated.foo", sleepAfter: TimeSpan.FromSeconds(0.5));
    await driver.SingleClickAsync(MyLibrary.Instance.Pages.Login.Password);
    await driver.TypeTextAsync("V3ry5ecre7!", sleepAfter: TimeSpan.FromSeconds(0.5));

    await driver.SingleClickAsync(MyLibrary.Instance.Pages.Login.Submit);
    
    // insert more magic here
}
```

The [sample project](https://github.com/asimmon/askaiser-marionette/tree/master/samples/Askaiser.Marionette.ConsoleApp) shows the basics of using this library.


## 🏭 Change the C# source generator behavior

Given the following partial image library class:

```csharp
[ImageLibrary("path/to/your/images/directory")]
public partial class MyLibrary
{
}

var library = new MyLibrary(); // or use MyLibrary.Instance generated singleton
```

The source generator will behave differently depending on the name of the screenshots/images added to the `ImageLibraryAttribute`'s directory path.

> **Use lowercase characters**. Dashes (`-`) and underscores (`_`) are considered as special characters that will change the generated C# code as shown below.

* Adding an image `okbutton.png` will create an image property `library.Okbutton`,
* Adding an image `ok-button.png` will create an image property `library.OkButton` (here the **dash** is a word separator),
* Adding an image in a **subdirectory** `my-feature\ok-button.png` will create an image property `library.MyFeature.OkButton`,
* Adding an image with a **double dash** `my-feature--ok-button.png` simulates a subdirectory and will create an image property `library.MyFeature.OkButton`.

Adding multiple images **suffixed with a zero-based incremental number** such as `ok-button_0.png`, `ok-button_1.png`, `ok-button_2.png`, etc. will create a single array property `library.OkButton` that can be interacted with methods that accept an array of elements (`MoveToAnyAsync`, `WaitForAnyAsync`, `SingleClickAnyAsync`, etc.).
It is very convenient when you have screenshots of an element in multiple states, for instance a button in its normal, pressed, focus state and you want to click on the button no matter what its current state is.

**Underscores** `_` can be also used to specify image recognition options:

* Adding an image `foo_gs.png` will create an image property `library.Foo` which will be grayscaled during image recognition,
* Adding an image `foo_0.8` will create an image property `library.Foo` that will use a search threshold of `0.8` instead of the default `0.95`,

You can mix these modifiers. Here we will create an single array property `library.Header` with these images:
* `header_gs_0.85_0.png` (first item of the array, grayscaled with a 0.85 threshold),
* `header_gs_0.9_1.png` (second item of the array, grayscaled with a 0.9 threshold),
* `header_2.png` (third and last item of the array, keep original colors with and use default threshold).


## ✍ Manually creating image and text elements

### Image search

```csharp
// Instead of relying on the source generator that works with image files, you can create an ImageElement manually
var bytes = await File.ReadAllBytesAsync("path/to/your/image.png");
var image = new ImageElement(name: "sidebar-close-button", content: bytes, threshold: 0.95m, grayscale: false);
```

* `ImageElement.Threshold` is a floating number between 0 and 1. It defines the accuracy of the image search process. `0.95` is the default value.
* `ImageElement.Grayscale` defines whether or not the engine will apply grayscaling preprocessing. Image search is faster with grayscaling.

**Image recognition works best with PNG images.**

### Text search

```csharp
// Although many methods accept a simple string as an element, you can manually create a TextElement
var text = new TextElement("Save changes", options: TextOptions.BlackAndWhite | TextOptions.Negative);
```

**Text options** are flags that define the preprocessing behavior of your monitor's screenshots before executing the OCR.
* `TextOptions.None` : do not use preprocessing,
* `TextOptions.Grayscale` : Use grayscaling,
* `TextOptions.BlackAndWhite` : Use grayscaling and binarization (this is the default value),
* `TextOptions.Negative` : Use negative preprocessing, very helpful with white text on dark background.


## 📕 API reference

Many parameters are optional. Most methods that look for an element (image or text) expect to find **only one occurrence** of this element. `ElementNotFoundException` and `MultipleElementFoundException` can be thrown.

You can use `DriverOptions.FailureScreenshotPath` to automatically save screenshots when these exceptions occur.

### Configuration and utilities

```csharp
static Create()
static Create(DriverOptions options)

GetScreenshotAsync()
SaveScreenshotAsync(Stream destinationStream)
SaveScreenshotAsync(string destinationPath)
GetCurrentMonitorAsync()
GetMonitorsAsync()
SetCurrentMonitor(int monitorIndex)
SetCurrentMonitor(MonitorDescription monitor)
SetMouseSpeed(MouseSpeed speed)
SleepAsync(int millisecondsDelay)
SleepAsync(TimeSpan delay)
```

### Basic methods

```csharp
WaitForAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
WaitForAllAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
WaitForAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
SingleClickAsync(int x, int y)
DoubleClickAsync(int x, int y)
TripleClickAsync(int x, int y)
RightClickAsync(int x, int y)
MoveToAsync(int x, int y)
DragFromAsync(int x, int y)
DropToAsync(int x, int y)
TypeTextAsync(string text, TimeSpan? sleepAfter)
KeyPressAsync(VirtualKeyCode[] keyCodes)
KeyDownAsync(VirtualKeyCode[] keyCodes)
KeyUpAsync(VirtualKeyCode[] keyCodes)
ScrollDownAsync(int scrollTicks)
ScrollUpAsync(int scrollTicks)
ScrollDownUntilVisibleAsync(IElement element, TimeSpan totalDuration, int scrollTicks, Rectangle? searchRect)
ScrollUpUntilVisibleAsync(IElement element, TimeSpan totalDuration, int scrollTicks, Rectangle? searchRect)
```

### Mouse interaction with an element

```csharp
MoveToAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
SingleClickAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
DoubleClickAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
TripleClickAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
RightClickAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
DragFromAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
DropToAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
```

### Check for element visibility

```csharp
IsVisibleAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
IsAnyVisibleAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
AreAllVisibleAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
```

### Mouse interaction with the first available element of a collection

```csharp
MoveToAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
SingleClickAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
DoubleClickAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
TripleClickAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
RightClickAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
DragFromAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
DropToAnyAsync(IEnumerable<IElement> elements, TimeSpan? waitFor, Rectangle? searchRect)
```

### Text-based actions

```csharp
WaitForAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
MoveToAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
SingleClickAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
DoubleClickAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
TripleClickAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
RightClickAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
DragFromAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
DropToAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
IsVisibleAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
```

### Mouse interaction with points

```csharp
MoveToAsync(Point coordinates)
SingleClickAsync(Point coordinates)
DoubleClickAsync(Point coordinates)
TripleClickAsync(Point coordinates)
RightClickAsync(Point coordinates)
DragFromAsync(Point coordinates)
DropToAsync(Point coordinates)
```

### Mouse interaction with `WaitFor` search result

```csharp
MoveToAsync(SearchResult searchResult)
SingleClickAsync(SearchResult searchResult)
DoubleClickAsync(SearchResult searchResult)
TripleClickAsync(SearchResult searchResult)
RightClickAsync(SearchResult searchResult)
DragFromAsync(SearchResult searchResult)
DropToAsync(SearchResult searchResult)
```

### Key press with single key code

```csharp
KeyPressAsync(VirtualKeyCode keyCode, TimeSpan? sleepAfter)
KeyDownAsync(VirtualKeyCode keyCode, TimeSpan? sleepAfter)
KeyUpAsync(VirtualKeyCode keyCode, TimeSpan? sleepAfter)
```

### `System.Drawing.Image`-based actions

```csharp
WaitForAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
MoveToAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
SingleClickAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
DoubleClickAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
TripleClickAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
RightClickAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
DragFromAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
DropToAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
IsVisibleAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
```

### Finding elements locations without throwing not found exceptions or multiple element found exceptions

```csharp
FindLocationsAsync(IElement element, TimeSpan? waitFor, Rectangle? searchRect)
FindLocationsAsync(string text, TimeSpan? waitFor, Rectangle? searchRect, TextOptions? textOptions)
FindLocationsAsync(Image image, TimeSpan? waitFor, Rectangle? searchRect, decimal? threshold, bool? grayscale)
```


## 🤖 Running tests in a CI environment

> 👷‍♂️ Work in progress

This section will soon show how Askaiser.Marionette can be used in an [Azure Pipelines](https://azure.microsoft.com/en-us/products/devops/pipelines/) continuous integration environment:

1. Setup a dedicated Windows agent, or use the [built-in Windows agent](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/agents?view=azure-devops&tabs=browser#microsoft-hosted-agents),
2. Use Microsoft's [Screen Resolution Utility task](https://marketplace.visualstudio.com/items?itemName=ms-autotest.screen-resolution-utility-task) to setup a virtual monitor and change its resolution.

There seems to be an [equivalent for GitHub actions](https://github.com/actions/runner-images/issues/2935). If you use another CI environment, please search its documentation for an equivalent behavior.
