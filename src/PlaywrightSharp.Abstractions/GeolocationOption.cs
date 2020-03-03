using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Geolocation option.
    /// </summary>
    /// <seealso cref="IBrowserContext.SetGeolocationAsync(GeolocationOption)"/>
    public class GeolocationOption : IEquatable<GeolocationOption>
    {
        /// <summary>
        /// Latitude between -90 and 90.
        /// </summary>
        /// <value>The latitude.</value>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude between -180 and 180.
        /// </summary>
        /// <value>The longitude.</value>
        public double Longitude { get; set; }

        /// <summary>
        /// Optional non-negative accuracy value.
        /// </summary>
        /// <value>The accuracy.</value>
        public double Accuracy { get; set; }

        /// <inheritdoc cref="IEquatable{T}"/>
        public bool Equals(GeolocationOption other)
            => other != null &&
                Latitude == other.Latitude &&
                Longitude == other.Longitude &&
                Accuracy == other.Accuracy;

        /// <inheritdoc cref="IEquatable{T}"/>
        public override bool Equals(object obj) => Equals(obj as GeolocationOption);

        /// <inheritdoc cref="IEquatable{T}"/>
        public override int GetHashCode()
            => (Latitude.GetHashCode() ^ 2014) +
                (Longitude.GetHashCode() ^ 2014) +
                (Accuracy.GetHashCode() ^ 2014);
    }
}
