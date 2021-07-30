using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands
{
    internal class WaitForAllCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForAllCommandHandler(DriverOptions options, IFileWriter fileWriter, IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(options, fileWriter, monitorService, elementRecognizer)
        {
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "This is safe because the cancellation token source's WaitHandle is not used.")]
        public async Task<SearchResultCollection> Execute(WaitForCommand command)
        {
            using var cts = new CancellationTokenSource();

            var tasks = command.Elements.Select(async element =>
            {
                try
                {
                    var disposableResult = await this.WaitFor(element, command, cts.Token).ConfigureAwait(false);
                    return await this.TrimRecognizerResultAndThrowIfRequired(command, disposableResult).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (!cts.IsCancellationRequested)
                    {
                        // This task have failed so we don't need to wait for the others as this exception will be rethrown.
                        cts.Cancel();
                    }

                    throw;
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return new SearchResultCollection(results);
        }
    }
}
