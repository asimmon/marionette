namespace Askaiser.Marionette;

/// <summary>
/// User-facing messages. They are kept in a single place so that wording stays consistent.
/// </summary>
internal static class Messages
{
    public const string MultipleElementFoundException_Message = "Element '{0}' was found at multiple locations: {1}.";
    public const string ElementNotFoundException_Message_WithDuration = "Element '{0}' was not found within the allotted time: {1}.";
    public const string ElementNotFoundException_Message_WithoutDuration = "Element '{0}' was not found.";
    public const string Point_ToString = "({0},{1})";
    public const string Rectangle_Throw_LeftGreaterThanRight = "Left property '{0}' cannot be greater than the Right property '{1}'.";
    public const string Rectangle_Throw_TopGreaterThanBottom = "Top property '{0}' cannot be greater than the Bottom property '{1}'.";
    public const string Rectangle_ToString = "({0},{1},{2},{3})";
    public const string MonitorDescription_ToString = "Monitor #{0} {1} {2}x{3}";
    public const string Image_Throw_RegionOutOfBounds = "The region {0} is not contained in the {1}x{2} image.";
    public const string Image_Throw_EmptyRegion = "The region cannot be empty.";
    public const string Image_Throw_InvalidDimensions = "Image dimensions must be greater than zero.";
    public const string Image_Throw_InvalidPixelLength = "Expected {0} bytes of pixel data for a {1}x{2} image, but got {3}.";
    public const string Image_Throw_CannotDecode = "The image could not be decoded. Supported formats are PNG, JPEG and BMP. {0}";
    public const string DriverOptions_Throw_NegativeScreenshotCacheDuration = "Screenshot cache duration cannot be negative.";
    public const string DriverOptions_Throw_NegativeWaitForThrottlingInterval = "'waitFor' throttling interval cannot be negative.";
    public const string DriverOptions_Throw_NegativeDefaultWaitForDuration = "Default 'waitFor' duration cannot be negative.";
    public const string DriverOptions_Throw_NegativeDefaultKeyboardSleepAfterDuration = "Default 'sleepAfter' duration cannot be negative.";
    public const string MarionetteDriver_Throw_ElementsEmpty = "Elements cannot be empty.";
    public const string MarionetteDriver_Throw_ScrollTicksNotGreaterThanZero = "Scroll ticks must be greater than zero.";
    public const string MarionetteDriver_Throw_EmptyKeyCodes = "Key codes cannot be empty.";
    public const string MarionetteDriver_Throw_UnsupportedElement = "Element '{0}' of type '{1}' cannot be recognized. Only image and text elements are supported.";
    public const string Throw_NegativeWaitFor = "Wait for duration cannot be negative.";
    public const string Element_Throw_ElementNotFound = "Element '{0}' was not found.";
    public const string Element_Throw_InvalidName = "Name cannot be null or empty.";
    public const string ImageElement_Throw_EmptyContent = "Content must be a non-empty array of bytes.";
    public const string ImageElement_Throw_ThresholdOutOfRange = "Threshold must be a floating number between 0 and 1.";
    public const string ImageElement_Throw_InvalidBase64 = "Content is not valid base64 text.";
    public const string ImageElement_Throw_NoFeatures = "Image element '{0}' is a single solid color. Image recognition needs an image with visible features.";
    public const string TextElement_Throw_EmptyContent = "Content cannot be null or empty.";
    public const string SearchResultCollection_Throw_ResultsEmpty = "Results cannot be empty.";
    public const string SearchResult_ToString = "Search results for '{0}': {1}";
    public const string FailureScreenshotFileNameFormat = "{0:yyyy-MM-dd-HH-mm-ss-ffff}_{1}.png";
    public const string MonitorService_Throw_MonitorNotFound = "Requested monitor index '{0}' not found.";
    public const string ElementCollectionExtensions_Throw_ElementAlreadyExists = "An element with the name '{0}' already exists.";
    public const string ElementCollectionExtensions_Throw_InvalidElementName = "An element cannot have a null or empty name.";
    public const string ElementCollectionExtensions_Throw_NullJsonElement = "The JSON document cannot contain null elements.";
    public const string ElementCollectionExtensions_Throw_UnsupportedElementType = "Element of type '{0}' is not supported.";
    public const string Platform_Throw_Unsupported = "Marionette supports Windows, Linux (X11) and macOS. The current operating system is not supported: {0}.";
    public const string Platform_Throw_KeyNotSupported = "The key '{0}' cannot be simulated on {1}.";
    public const string Platform_Throw_X11DisplayUnavailable = "Could not open the X11 display '{0}'. Marionette needs an X11 session on Linux; make sure the DISPLAY environment variable is set. On Wayland, run the tested application and the tests under XWayland or in a virtual X server such as Xvfb.";
    public const string Platform_Throw_X11ExtensionMissing = "The X server does not provide the '{0}' extension, which is required to simulate input.";
    public const string Platform_Throw_LibraryMissing = "The native library '{0}' could not be loaded. {1}";
    public const string Platform_Throw_CaptureFailed = "The screen could not be captured. {0}";
    public const string Platform_MacOS_ScreenRecordingHint = "On macOS, grant the 'Screen Recording' permission to the application or terminal running the tests in System Settings > Privacy & Security.";
    public const string Platform_MacOS_AccessibilityHint = "On macOS, grant the 'Accessibility' permission to the application or terminal running the tests in System Settings > Privacy & Security.";
    public const string TextRecognition_Throw_ModelLoadFailed = "The text recognition model could not be loaded. {0}";
    public const string TextRecognition_Throw_FileNotFound = "The text recognition model file '{0}' does not exist.";
    public const string TextRecognition_Throw_Unavailable = "Text recognition is not available on this platform because ONNX Runtime could not be loaded ({0}). Image recognition is not affected.";
}
