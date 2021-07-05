using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal class WaitForCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForCommandHandler(IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(monitorService, elementRecognizer)
        {
        }

        public async Task<SearchResult> Execute(WaitForCommand command)
        {
            var result = await this.WaitFor(command.Elements.First(), command.Duration, command.SearchRectangle, command.MonitorIndex).ConfigureAwait(false);
            result.EnsureSingleLocation();
            return result;
        }
    }
}