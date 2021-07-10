using System;
using Askaiser.UITesting;
using System.Linq;

var opts = new TestContextOptions
{
    FailureScreenshotPath = @"C:\Users\simmo\Desktop\failures"
};

using var ctx = TestContext.Create(opts);

var monitors = await ctx.GetMonitors();
var monitor = monitors.First();

// await ctx.SingleClick("File", waitFor: TimeSpan.FromSeconds(1), searchRect: monitor.FromTopLeft(100, 100), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
// await ctx.SingleClick("Settings", waitFor: TimeSpan.FromSeconds(2), searchRect: monitor.FromTopLeft(500, 200), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
// await ctx.SingleClick("Manage layers", waitFor: TimeSpan.FromSeconds(10), searchRect: monitor.FromCenter(500, 500), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);

await ctx.IsVisible("get started now", waitFor: TimeSpan.FromSeconds(0), searchRect: new Rectangle(1153, 478, 1574, 615), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
await ctx.MoveTo("get started now", waitFor: TimeSpan.FromSeconds(0), searchRect: new Rectangle(1153, 478, 1574, 615), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
