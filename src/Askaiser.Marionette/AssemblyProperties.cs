using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Askaiser.Marionette.Tests")]
[assembly: InternalsVisibleTo("Askaiser.Marionette.Benchmarks")]

// Native libraries are system libraries: never look for them next to the application.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
