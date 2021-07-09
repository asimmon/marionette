using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Askaiser.UITesting
{
    public sealed class SearchResult : IEnumerable<Rectangle>
    {
        public IElement Element { get; }

        public IReadOnlyList<Rectangle> Areas { get; }

        public bool Success { get; }

        public Rectangle Area
        {
            get
            {
                this.EnsureSingleLocation();
                return this.Areas[0];
            }
        }

        internal SearchResult(IElement element, IEnumerable<Rectangle> areas)
        {
            this.Element = element;
            this.Areas = new List<Rectangle>(areas);
            this.Success = this.Areas.Count > 0;
        }

        public IEnumerator<Rectangle> GetEnumerator()
        {
            return this.Areas.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal SearchResult AdjustToMonitor(MonitorDescription monitor)
        {
            var newAreas = this.Areas.Select(x => x.AddOffset(monitor.Left, monitor.Top));
            return new SearchResult(this.Element, newAreas);
        }

        internal SearchResult AdjustToSearchRectangle(Rectangle rect)
        {
            if (rect == null)
                return this;

            var newAreas = this.Areas.Select(x => x.AddOffset(rect.Left, rect.Top));
            return new SearchResult(this.Element, newAreas);
        }

        public override string ToString()
        {
            if (!this.Success)
                return $"Element {this.Element} was not found.";

            var sb = new StringBuilder();

            sb.Append("Element ").Append(this.Element.Name).Append(" was found at location");

            if (this.Areas.Count > 1)
                sb.Append('s');

            sb.Append(": ");
            this.SerializeAreasTo(sb);
            sb.Append('.');

            return sb.ToString();
        }

        public static SearchResult NotFound(IElement element)
        {
            return new SearchResult(element, Enumerable.Empty<Rectangle>());
        }

        public void EnsureSingleLocation()
        {
            if (this.Areas.Count == 1)
                return;

            if (this.Areas.Count == 0)
                throw new InvalidOperationException($"No location was found for element {this.Element}.");

            var sb = new StringBuilder("Multiple location were found for element ")
                .Append(this.Element)
                .Append(": ");

            this.SerializeAreasTo(sb);
            throw new InvalidOperationException(sb.ToString());
        }

        private void SerializeAreasTo(StringBuilder sb)
        {
            for (var i = 0; i < this.Areas.Count; i++)
            {
                var (x, y) = this.Areas[i].Center;
                sb.Append('(').Append(x).Append(", ").Append(y).Append(')');

                if (i != this.Areas.Count - 1)
                    sb.Append(", ");
            }
        }
    }
}
