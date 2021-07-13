﻿using System;
using Askaiser.Puppets;
using System.Linq;

var opts = new TestContextOptions
{
    FailureScreenshotPath = @"C:\Users\simmo\Desktop\failures"
};

using var ctx = TestContext.Create(opts);

var monitors = await ctx.GetMonitors();
var monitor = monitors.First();

await ctx.SingleClick("File", waitFor: TimeSpan.FromSeconds(1), searchRect: monitor.FromTopLeft(100, 100), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
// await ctx.SingleClick("Settings", waitFor: TimeSpan.FromSeconds(2), searchRect: monitor.FromTopLeft(500, 200), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
// await ctx.SingleClick("Manage layers", waitFor: TimeSpan.FromSeconds(10), searchRect: monitor.FromCenter(500, 500), textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);

