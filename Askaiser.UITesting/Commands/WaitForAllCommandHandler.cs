using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal class WaitForAllCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForAllCommandHandler(IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(monitorService, elementRecognizer)
        {
        }

        public async Task<SearchResultCollection> Execute(WaitForCommand command)
        {
            var tasks = command.Elements.Select(async element =>
            {
                var result = await this.WaitFor(element, command.Duration, command.MonitorIndex).ConfigureAwait(false);
                result.EnsureSingleLocation();
                return result;
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return new SearchResultCollection(results);
        }
    }
}