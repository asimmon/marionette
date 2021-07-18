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
                await ctx.MoveToAny(new[] { library.RiderLogo, library.VsLogo }, waitFor: TimeSpan.FromSeconds(2));
            }
        }
    }
}
