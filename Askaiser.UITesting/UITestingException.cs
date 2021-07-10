using System;

namespace Askaiser.UITesting
{
    internal abstract class UITestingException : Exception
    {
        protected UITestingException(string message)
            : base(message)
        {
        }
    }
}
