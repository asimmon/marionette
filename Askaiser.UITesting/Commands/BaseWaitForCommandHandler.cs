using System;
using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal abstract class BaseWaitForCommandHandler
    {
        private readonly IMonitorService _monitorService;
        private readonly IElementRecognizer _elementRecognizer;

        protected BaseWaitForCommandHandler(IMonitorService monitorService, IElementRecognizer elementRecognizer)
        {
            this._monitorService = monitorService;
            this._elementRecognizer = elementRecognizer;
        }

        protected async Task<SearchResult> WaitFor(IElement element, TimeSpan duration, int monitorIndex)
        {
            return await ElementAwaiter.WaitFor(this._monitorService, this._elementRecognizer, element, duration, monitorIndex).ConfigureAwait(false);
        }
    }
}