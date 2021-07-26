using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands
{
    internal class WaitForAnyCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForAnyCommandHandler(DriverOptions options, IFileWriter fileWriter, IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(options, fileWriter, monitorService, elementRecognizer)
        {
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "This is safe because the cancellation token source's WaitHandle is not used.")]
        public async Task<SearchResult> Execute(WaitForCommand command)
        {
            using var cts = new CancellationTokenSource();

            var tasks = command.Elements.Select(async element =>
            {
                var result = await this.WaitFor(element, command, cts.Token).ConfigureAwait(false);
                if (result.Locations.Count == 1)
                {
                    //TODO wip
                }
                return result;
            });

            var firstTaskToFinish = await Task.WhenAny(tasks).ConfigureAwait(false);

            // Rethrow exception if all the tasks are faulted
            if (firstTaskToFinish.IsFaulted)
            {
                await firstTaskToFinish.ConfigureAwait(false);
            }

            return firstTaskToFinish.Result;
        }
    }
}
