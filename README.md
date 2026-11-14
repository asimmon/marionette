# Askaiser.Marionette

Marionette automates anything that can be displayed on a screen: it **finds images and text** (OCR), then **clicks and types**. It is not tied to a browser or a UI toolkit, so it works with desktop applications, games, remote desktops, virtual machines and web pages alike.

It runs on **Windows, Linux (X11) and macOS** with .NET 10 or later.

```csharp
using var driver = MarionetteDriver.Create();

await driver.SingleClickAsync("Sign in");                       // read the screen, click the text
await driver.SingleClickAsync(MyLibrary.Instance.LoginButton);  // or an image from your own library
await driver.TypeTextAsync("jane.doe@example.com");
await driver.KeyPressAsync(VirtualKeyCode.Enter);
await driver.WaitForAsync("Welcome back");
```

## Installation

```
dotnet add package Askaiser.Marionette
```

The package contains the library, the text recognition models and the image library source generator. There is nothing else to install on Windows. On the other platforms:

| Platform | Requirements |
| --- | --- |
| Linux | An X11 session (or XWayland, which Wayland desktops provide) and the `libX11`, `libXext`, `libXtst` and `libXrandr` libraries. In CI, use a virtual display: `xvfb-run -s "-screen 0 1920x1080x24" dotnet test`. |
| macOS | Grant **Screen Recording** and **Accessibility** to the terminal or IDE that runs your program, in System Settings → Privacy & Security. Marionette reports a clear exception when a permission is missing. |
| Windows | Nothing. Marionette is DPI aware and works with any scaling. Run it in an interactive desktop session. |

## Finding things

Everything is searched on the current monitor by default; pass a `searchRect` to search a smaller area (much faster) and `waitFor` to wait for an element to appear.

```csharp
await driver.IsVisibleAsync("Settings");
await driver.WaitForAsync("Saved", waitFor: TimeSpan.FromSeconds(10));
var matches = await driver.FindLocationsAsync("File");            // every occurrence of a word
await driver.SingleClickAnyAsync(new IElement[] { okButton, yesButton }); // whichever shows up
```

Elements are text (`TextElement`, or just a `string`) or images (`ImageElement`). Images are matched by normalized cross-correlation, with an optional threshold and grayscale mode; the search is exact and fast: a typical icon is found on a 1080p screen in about 15 ms. Text is detected and read by a bundled neural OCR (PP-OCR on ONNX Runtime), which also tells where each character is, so you can click a single word inside a sentence. Text is matched whatever its case, unless `TextElement.IgnoreCase` is false. `TextOptions` preprocess the screen before it is read (grayscale, black and white, negative), which is rarely needed.

When an element is not found, a screenshot of what was searched is written to `DriverOptions.FailureScreenshotPath`.

### Cancellation

Every asynchronous method takes an optional `CancellationToken` as its last parameter, and throws an `OperationCanceledException` when it is canceled. Waiting, searching and moving the mouse stop right away.

```csharp
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));

await driver.WaitForAsync("Welcome back", waitFor: TimeSpan.FromSeconds(30), cancellationToken: timeout.Token);
await driver.MoveToAsync(200, 300, timeout.Token);
```

In a test, pass the token of your test framework: a test that times out or is aborted then stops driving the desktop instead of clicking around while the next test runs. Cancellation never leaves the desktop in a state you did not ask for: a canceled click releases the mouse button, and a canceled shortcut releases its keys.

### Languages

The default text model reads Latin script (English, French, German, Spanish, Portuguese, Italian and others, with diacritics). To read another script, give a PP-OCR recognition model in ONNX format and its character list:

```csharp
var options = new DriverOptions { TextRecognitionModel = TextRecognitionModel.FromFiles(recognitionModelPath, dictionaryPath) };
```

## Image libraries

Declare a partial class pointing to a directory of images. A source generator turns every file into a property, and the images are embedded in your assembly:

```csharp
[ImageLibrary("images")]
public partial class MyLibrary { }

await driver.SingleClickAsync(MyLibrary.Instance.Logo);
```

| File | Generated member |
| --- | --- |
| `save-button.png` | `SaveButton` |
| `login--user-name.png` or `login/user-name.png` | `Login.UserName`, in a nested library class |
| `logo_0.png`, `logo_1.png` | `Logo`, an array of two alternatives (any of them matches) |
| `icon_gs.png` | `Icon`, compared in grayscale |
| `icon_0.8.png` | `Icon` with a similarity threshold of 0.8 |

The generator only lists files while you type and only reads images that changed, so it does not slow down the IDE. Problems are reported as diagnostics `AMSG001` to `AMSG009` (missing directory, duplicate names, unreadable image, and so on).

## What changed since 1.x

The public API is the same (`MarionetteDriver`, its extension methods, `ImageElement`, `TextElement`, `ElementCollection`, `SearchResult`, `[ImageLibrary]`…), apart from the following, which were necessary to be cross-platform:

- **`System.Drawing.Bitmap` is replaced by `Askaiser.Marionette.Image`**, a small BGRA image type (`FromFile`, `FromBytes`, `Crop`, `ToPng`, `Save`…). `System.Drawing` is Windows only.
- **`VirtualKeyCode` keeps its names and values** but drops keys that have no meaning on every platform (IME keys, mouse buttons, `Sleep`…) and adds `LeftCommand`, `RightCommand` and `ControlOrCommand`, which is Control on Windows and Linux and Command on macOS.
- **Tesseract and OpenCV are gone**, so are `tessdata` files and the Tesseract options of `DriverOptions`; `TextRecognitionModel` replaces them. `TextOptions` now defaults to `None`.
- **.NET 10+ only**, and the library is trim and AOT compatible.
- **Asynchronous methods accept a `CancellationToken`**, as their last and optional parameter.

Under the hood the image search is lossless but several times faster, the text recognition is more accurate and needs no downloads, and screenshots are pooled to keep memory low.

## Building

```
dotnet build Askaiser.Marionette.slnx
dotnet test --solution Askaiser.Marionette.slnx
```

Desktop tests, which drive the real screen, mouse and keyboard, only run when `MARIONETTE_DESKTOP_TESTS=1`. A browser (Chrome or Edge) must be installed.

## License

GPL-3.0, see [LICENSE](LICENSE). Third-party components are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
