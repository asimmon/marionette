using System;

namespace Askaiser.UITesting
{
    internal sealed class WaitForTimeoutException : TimeoutException
    {
        public WaitForTimeoutException(IElement element, TimeSpan duration)
            : base($"Element {element} not found within the allotted time: " + duration)
        {
        }
    }
}
