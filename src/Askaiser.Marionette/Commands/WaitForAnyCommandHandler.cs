using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands
{
    internal class WaitForAnyCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForAnyCommandHandler(TestContextOptions options, IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(options, monitorService, elementRecognizer)
        {
        }

        public async Task<SearchResult> Execute(WaitForCommand command)
        {
            var tasks = command.Elements.Select(async x => await this.WaitFor(x, command).ConfigureAwait(false));
            var firstTaskToFinish = await Task.WhenAny(tasks).ConfigureAwait(false);

            // Rethrow exception if all the tasks are faulted
            if (firstTaskToFinish.IsFaulted)
                await firstTaskToFinish.ConfigureAwait(false);

            return firstTaskToFinish.Result;
        }
    }
}
