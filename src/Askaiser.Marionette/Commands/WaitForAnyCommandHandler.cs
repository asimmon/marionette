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
                try
                {
                    return await this.WaitFor(element, command, cts.Token).ConfigureAwait(false);
                }
                finally
                {
                    if (!cts.IsCancellationRequested)
                    {
                        // This task is the first one to finish so we can cancel the others
                        cts.Cancel();
                    }
                }
            });

            var firstTaskToFinish = await Task.WhenAny(tasks).ConfigureAwait(false);

            // Rethrow any exception
            if (firstTaskToFinish.IsFaulted)
            {
                await firstTaskToFinish.ConfigureAwait(false);
            }

            return await this.TrimRecognizerResultAndThrowIfRequired(command, firstTaskToFinish.Result).ConfigureAwait(false);
        }
    }
}
