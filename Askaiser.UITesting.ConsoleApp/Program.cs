using System;
using Askaiser.UITesting;
using System.Linq;

using var ctx = TestContext.Create();

var element = new TextElement("Amazon Music", TextOptions.BlackAndWhite | TextOptions.Negative);

var monitors = await ctx.GetMonitors();
var monitor = monitors.First();

await ctx.SingleClick(monitor.Left, monitor.Bottom);
await ctx.MoveTo(element, waitFor: TimeSpan.FromSeconds(1), searchRect: monitor.FromBottomLeft(200, 200));
