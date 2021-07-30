using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Askaiser.Marionette
{
    public class SearchResult : IEnumerable<Rectangle>
    {
        public IElement Element { get; }

        public IReadOnlyList<Rectangle> Locations { get; }

        public bool Success { get; }

        public Rectangle Location
        {
            get
            {
                this.EnsureSingleLocation(TimeSpan.Zero);
                return this.Locations[0];
            }
        }

        internal SearchResult(IElement element, IEnumerable<Rectangle> locations)
        {
            this.Element = element;
            this.Locations = new List<Rectangle>(locations);
            this.Success = this.Locations.Count > 0;
        }

        internal SearchResult(SearchResult other)
            : this(other.Element, other.Locations)
        {
        }

        public IEnumerator<Rectangle> GetEnumerator()
        {
            return this.Locations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal SearchResult AdjustToMonitor(MonitorDescription monitor)
        {
            var newLocations = this.Locations.Select(x => x + (monitor.Left, monitor.Top));
            return new SearchResult(this.Element, newLocations);
        }

        internal SearchResult AdjustToSearchRectangle(Rectangle rect)
        {
            if (rect == null)
            {
                return this;
            }

            var newLocations = this.Locations.Select(x => x + (rect.Left, rect.Top));
            return new SearchResult(this.Element, newLocations);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Search results for '{0}': {1}", this.Element, this.Locations.Count > 0 ? this.Locations.ToCenterString() : "none");
        }

        public static SearchResult NotFound(IElement element)
        {
            return new SearchResult(element, Enumerable.Empty<Rectangle>());
        }

        public void EnsureSingleLocation(TimeSpan waitFor)
        {
            if (this.Locations.Count == 0)
            {
                throw new ElementNotFoundException(this.Element, waitFor);
            }

            if (this.Locations.Count > 1)
            {
                throw new MultipleElementFoundException(this);
            }
        }
    }
}
