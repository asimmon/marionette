using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Askaiser.Marionette
{
    public interface IScreenshotProvider : IDisposable
    {
        Task<Bitmap> GetScreenshot();

        Task<int> GetScreenWidth();

        Task<int> GetScreenHeight();
    }
}
