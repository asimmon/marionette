using System;

namespace Askaiser.Marionette.SourceGenerator.Tests
{
    public class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTime now)
        {
            this.Now = now;
        }

        public DateTime Now { get; set; }
    }
}
