namespace Snyk.Code.Library.Domain.Analysis
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// <see cref="Marker"/> comparer by position line.
    /// </summary>
    public class MarkerByPositionLineComparer : IEqualityComparer<Marker>
    {
        /// <summary>
        /// Compare two markers by first position line number.
        /// </summary>
        /// <param name="markerX">First marker.</param>
        /// <param name="markerY">Second marker.</param>
        /// <returns>True if marker start position equal.</returns>
        public bool Equals(Marker markerX, Marker markerY)
        {
            if (object.ReferenceEquals(markerX, markerY))
            {
                return true;
            }

            if (object.ReferenceEquals(markerX, null) || object.ReferenceEquals(markerY, null))
            {
                return false;
            }

            return markerX.Positions[0].Rows.ElementAt(0) == markerY.Positions[0].Rows.ElementAt(0);
        }

        /// <summary>
        /// Generate Hash code for <see cref="Marker"/>.
        /// </summary>
        /// <param name="marker">Source marker object.</param>
        /// <returns>Hash code for marker.</returns>
        public int GetHashCode(Marker marker)
        {
            if (object.ReferenceEquals(marker, null))
            {
                return 0;
            }

            return marker.Positions[0].Rows.ElementAt(0).GetHashCode();
        }
    }
}
