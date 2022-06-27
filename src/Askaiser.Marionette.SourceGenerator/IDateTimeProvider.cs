using System;

namespace Askaiser.Marionette.SourceGenerator;

internal interface IDateTimeProvider
{
    public DateTime Now { get; }
}
