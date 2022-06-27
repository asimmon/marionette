using System;

namespace Askaiser.Marionette.SourceGenerator;

public class UtcDateTimeProvider : IDateTimeProvider
{
    public DateTime Now
    {
        get => DateTime.UtcNow;
    }
}
