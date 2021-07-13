using System;

namespace Askaiser.Puppets
{
    public abstract class UITestingException : Exception
    {
        protected UITestingException(string message)
            : base(message)
        {
        }
    }
}
