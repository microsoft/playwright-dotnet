using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Geolocation option.
    /// </summary>
    /// <seealso cref="IBrowserContext.SetGeolocationAsync(Geolocation)"/>
    public class Geolocation : IEquatable<Geolocation>
    {
        /// <summary>
        /// Latitude between -90 and 90.
        /// </summary>
        public decimal Latitude { get; set; }

        /// <summary>
        /// Longitude between -180 and 180.
        /// </summary>
        public decimal Longitude { get; set; }

        /// <summary>
        /// Optional non-negative accuracy value.
        /// </summary>
        public decimal? Accuracy { get; set; }

        /// <inheritdoc cref="IEquatable{T}"/>
        public bool Equals(Geolocation other)
            => other != null &&
                Latitude == other.Latitude &&
                Longitude == other.Longitude &&
                Accuracy == other.Accuracy;

        /// <inheritdoc cref="IEquatable{T}"/>
        public override bool Equals(object obj) => Equals(obj as Geolocation);

        /// <inheritdoc cref="IEquatable{T}"/>
        public override int GetHashCode()
            => (Latitude.GetHashCode() ^ 2014) +
                (Longitude.GetHashCode() ^ 2014) +
                (Accuracy.GetHashCode() ^ 2014);

        /// <summary>
        /// Clones the <see cref="Geolocation"/>.
        /// </summary>
        /// <returns>A copy of the current <see cref="Geolocation"/>.</returns>
        public Geolocation Clone() => (Geolocation)MemberwiseClone();
    }
}
