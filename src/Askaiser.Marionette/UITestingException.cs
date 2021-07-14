using System;

namespace Askaiser.Marionette
{
    public abstract class UITestingException : Exception
    {
        protected UITestingException(string message)
            : base(message)
        {
        }
    }
}
