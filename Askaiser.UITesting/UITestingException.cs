using System;

namespace Askaiser.UITesting
{
    internal abstract class UITestingException : Exception
    {
        protected UITestingException(string message)
            : base(message)
        {
        }

        protected UITestingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
