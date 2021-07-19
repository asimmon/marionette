using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands
{
    internal class WaitForAllCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForAllCommandHandler(DriverOptions options, IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(options, monitorService, elementRecognizer)
        {
        }

        public async Task<SearchResultCollection> Execute(WaitForCommand command)
        {
            var tasks = command.Elements.Select(async x => await this.WaitFor(x, command).ConfigureAwait(false));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return new SearchResultCollection(results);
        }
    }
}
