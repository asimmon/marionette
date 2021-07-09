using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.UITesting.ConsoleApp
{
    public static class Program
    {
        public static async Task Main()
        {
            var ctx = TestContext.Create();

            var element = new TextElement("Amazon Music", TextOptions.BlackAndWhite | TextOptions.Negative)
            {
                IgnoreCase = true,
            };

            var monitors = await ctx.GetMonitors();
            var monitor = monitors.First();
            var searchRect = monitor.FromBottomLeft(200, 200);

            await ctx.SingleClick(monitor.Left, monitor.Bottom);
            await ctx.Sleep(500);
            await ctx.MoveTo(element, searchRect: searchRect);
        }
    }
}
