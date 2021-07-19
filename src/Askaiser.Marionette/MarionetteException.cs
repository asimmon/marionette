using System;

namespace Askaiser.Marionette
{
    public abstract class MarionetteException : Exception
    {
        protected MarionetteException(string message)
            : base(message)
        {
        }
    }
}
