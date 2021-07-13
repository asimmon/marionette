using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.Puppets.Commands
{
    internal class WaitForCommandHandler : BaseWaitForCommandHandler
    {
        public WaitForCommandHandler(TestContextOptions options, IMonitorService monitorService, IElementRecognizer elementRecognizer)
            : base(options, monitorService, elementRecognizer)
        {
        }

        public async Task<SearchResult> Execute(WaitForCommand command)
        {
            return await this.WaitFor(command.Elements.First(), command).ConfigureAwait(false);
        }
    }
}
