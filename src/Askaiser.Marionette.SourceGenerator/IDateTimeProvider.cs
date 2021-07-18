using System;

namespace Askaiser.Marionette.SourceGenerator
{
    interface IDateTimeProvider
    {
        public DateTime Now { get; }
    }
}