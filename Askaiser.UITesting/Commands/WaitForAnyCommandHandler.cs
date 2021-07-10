using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal class WaitForAnyCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForAnyCommandHandler(TestContextOptions options, IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(options, monitorService, elementRecognizer)
        {
        }

        public async Task<SearchResult> Execute(WaitForCommand command)
        {
            var tasks = command.Elements.Select(async element =>
            {
                var result = await this.WaitFor(element, command).ConfigureAwait(false);
                result.EnsureSingleLocation();
                return result;
            });

            var firstTaskToFinish = await Task.WhenAny(tasks).ConfigureAwait(false);

            // Rethrow exception if all the tasks are faulted
            if (firstTaskToFinish.IsFaulted)
                await firstTaskToFinish.ConfigureAwait(false);

            return firstTaskToFinish.Result;
        }
    }
}
