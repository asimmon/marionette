using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Askaiser.Marionette.Commands
{
    internal class WaitForCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForCommandHandler(DriverOptions options, IFileWriter fileWriter, IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(options, fileWriter, monitorService, elementRecognizer)
        {
        }

        public async Task<SearchResult> Execute(WaitForCommand command)
        {
            var disposableResult = await this.WaitFor(command.Elements.First(), command, CancellationToken.None).ConfigureAwait(false);
            return await this.TrimRecognizerResultAndThrowIfRequired(command, disposableResult).ConfigureAwait(false);
        }
    }
}
