using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Askaiser.Marionette.ConsoleApp
{
    [ImageLibrary("images")]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleType", Justification = "It shows how to build an image library in the main Program class.")]
    public partial class MyLibrary
    {
    }

    public static class Program
    {
        public static async Task Main()
        {
            var library = new MyLibrary();

            using (var ctx = TestContext.Create())
            {
                // We expect the IDE logo to be in a 200x200 square at the top left of the current monitor.
                var monitor = await ctx.GetCurrentMonitor();
                var monitorRect = new Rectangle(0, 0, monitor.Width, monitor.Height);
                var ideLogoRect = monitorRect.FromTopLeft(200, 200);

                await ctx.MoveToAny(new[] { library.RiderLogo, library.VsLogo }, waitFor: TimeSpan.FromSeconds(2), searchRect: ideLogoRect);

                // Also, in the same area, we expect to find the toolbar item "Edit". Negative preprocessing should be used if the IDE use a dark theme.
                await ctx.MoveTo("Edit", searchRect: ideLogoRect, textOptions: TextOptions.BlackAndWhite | TextOptions.Negative);
            }
        }
    }
}
