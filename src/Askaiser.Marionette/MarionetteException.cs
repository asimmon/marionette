using System;

namespace Askaiser.Marionette;

/// <summary>
/// The base class of the exceptions thrown when a search does not find an element exactly once.
/// </summary>
public abstract class MarionetteException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarionetteException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected MarionetteException(string message)
        : base(message)
    {
    }
}
