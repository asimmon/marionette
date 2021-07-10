using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.UITesting.Commands
{
    internal class WaitForCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForCommandHandler(TestContextOptions options, IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(options, monitorService, elementRecognizer)
        {
        }

        public async Task<SearchResult> Execute(WaitForCommand command)
        {
            var result = await this.WaitFor(command.Elements.First(), command).ConfigureAwait(false);
            result.EnsureSingleLocation();
            return result;
        }
    }
}
