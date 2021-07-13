using System;

namespace Askaiser.UITesting
{
    public abstract class UITestingException : Exception
    {
        protected UITestingException(string message)
            : base(message)
        {
        }
    }
}
