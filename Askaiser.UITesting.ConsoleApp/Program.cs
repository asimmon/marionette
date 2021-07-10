using System;
using Askaiser.UITesting;
using System.Linq;

using var ctx = TestContext.Create();

var monitors = await ctx.GetMonitors();
var monitor = monitors.First();

await ctx.SingleClick("File", waitFor: TimeSpan.FromSeconds(1), searchRect: monitor.FromTopLeft(100, 100), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
await ctx.SingleClick("Settings", waitFor: TimeSpan.FromSeconds(2), searchRect: monitor.FromTopLeft(500, 200), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
