using System;
using System.Threading.Tasks;

namespace Askaiser.UITesting.ConsoleApp
{
    public static class Program
    {
        public static async Task Main()
        {
            //*
            var elements = await new ElementCollection().LoadAsync(@"C:\Users\simmo\Desktop\elements.txt");

            var trayOpener = elements["tray-opener"];
            var riderLogo = elements["rider-logo"];
            var sidebarRecycleBin = elements["sidebar-recycle-bin"];
            var desktopLoulouwhat = elements["desktop-loulouwhat"];

            using var context = TestContext.Create();
            context.SetMonitor(1);

            await context.MoveTo(riderLogo, searchRect: new Rectangle(860, 440, 1060, 640), waitFor: TimeSpan.FromSeconds(2));

            // await context.ScrollUpUntilVisible(desktopLoulouwhat, TimeSpan.FromSeconds(10));
            // await context.Drag(desktopLoulouwhat);
            // await context.MoveTo(trayOpener);
            // await context.Drop(sidebarRecycleBin);

            //*/
        }
    }
}