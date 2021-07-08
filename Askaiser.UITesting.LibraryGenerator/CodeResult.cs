using System.Collections.Generic;
using System.Linq;

namespace Askaiser.UITesting.LibraryGenerator
{
    public sealed class CodeResult
    {
        public CodeResult(string code, IEnumerable<string> warnings)
        {
            this.Code = code;
            this.Warnings = warnings.ToList();
        }

        public string Code { get; }

        public IReadOnlyCollection<string> Warnings { get; }
    }
}