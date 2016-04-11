/*
 *  Copyright © 2015 Russell Libby
 */
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Services.Maps;

namespace SunMoonBandCommon
{
    /// <summary>
    /// Enumeration for point type.
    /// </summary>
    public enum PointType
    {
        /// <summary>
        /// Latitude.
        /// </summary>
        Lat,

        /// <summary>
        /// Longitude.
        /// </summary>
        Lon
    }

    /// <summary>
    /// Class for maintaining the degree, minute and second for latitude or longitude.
    /// </summary>
    public sealed class DmsPoint
    {
        #region Private fields

        private int _degrees;

        #endregion

        #region Public methods

        /// <summary>
        /// Override for ToString() handling.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var direction = (Orientation == PointType.Lat) ? (Degrees < 0 ? "S" : "N") : (Degrees < 0 ? "W" : "E");

            return String.Format(@"{0}° {1} {2} {3}", Math.Abs(Degrees), Minutes, Seconds, direction);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The degrees.
        /// </summary>
        public int Degrees
        {
            get { return Math.Abs(_degrees); }
            set { _degrees = value; }
        }

        /// <summary>
        /// The minutes.
        /// </summary>
        public int Minutes { get; set; }

        /// <summary>
        /// The seconds.
        /// </summary>
        public int Seconds { get; set; }

        /// <summary>
        /// Returns 1 or -1 to indicate the sign of the point.
        /// </summary>
        public int Sign
        {
            get { return (_degrees < 0) ? (-1) : 1; }
        }

        /// <summary>
        /// Latitude or longitude.
        /// </summary>
        public PointType Orientation { get; set; }

        #endregion
    }

    /// <summary>
    /// Wrapper for degree, minute, second location handling.
    /// </summary>
    public sealed class DmsLocation
    {
        #region Private fields

        private readonly DmsPoint _latitude;
        private readonly DmsPoint _longitude;

        #endregion

        #region Private methods

        /// <summary>
        /// Creates a new dms point value based on the decimal location.
        /// </summary>
        /// <param name="value">The decimal location value.</param>
        /// <param name="orientation">The point type that the decimal location refers to.</param>
        private DmsPoint ExtractDmsPoint(double value, PointType orientation)
        {
            var dmsPoint = new DmsPoint
            {
                Degrees = ExtractDegrees(value),
                Minutes = ExtractMinutes(value),
                Seconds = ExtractSeconds(value),
                Orientation = orientation
            };

            return dmsPoint;
        }

        /// <summary>
        /// Extracts the degrees from the decimal location.
        /// </summary>
        /// <param name="value">The decimal location.</param>
        /// <returns>The degrees.</returns>
        private static int ExtractDegrees(double value)
        {
            return (int)value;
        }

        /// <summary>
        /// Extracts the minutes from the decimal location.
        /// </summary>
        /// <param name="value">The decimal location.</param>
        /// <returns>The minutes.</returns>
        private static int ExtractMinutes(double value)
        {
            value = Math.Abs(value);

            return (int)((value - ExtractDegrees(value)) * 60);
        }

        /// <summary>
        /// Extracts the seconds from the decimal location.
        /// </summary>
        /// <param name="value">The decimal location.</param>
        /// <returns>The seconds.</returns>
        private static int ExtractSeconds(double value)
        {
            value = Math.Abs(value);

            var minutes = (value - ExtractDegrees(value)) * 60;
            
            return (int)Math.Round((minutes - ExtractMinutes(value)) * 60);
        }

        /// <summary>
        /// Get the city or town at a specified geo point.
        /// </summary>
        /// <param name="point">The point to reverse geocode.</param>
        /// <returns>The address if found, otherwise null.</returns>
        private static async Task<string> GetLocationNamePrivate(Geopoint point)
        {
            string result;

            try
            {
                var locations = await MapLocationFinder.FindLocationsAtAsync(point);

                result = (locations.Status != MapLocationFinderStatus.Success) ? "Unknown" : locations.Locations[0].Address.Town;
            }
            catch (Exception)
            {
                result = "Unknown";
            }

            return result;
        }

        /// <summary>
        /// Attempts to get the current location.
        /// </summary>
        /// <returns></returns>
        private static Geopoint GetLocationPrivate()
        {
            Geopoint result = null;

            var locater = new Geolocator
            {
                DesiredAccuracy = PositionAccuracy.Default,
                DesiredAccuracyInMeters = 1000,
                ReportInterval = 1000
            };

            if (locater.LocationStatus == PositionStatus.Disabled) return null;

            using (var waiter = new ManualResetEvent(false))
            {
                TypedEventHandler<Geolocator, PositionChangedEventArgs> handler = (sender, args) =>
                {
                    result = args.Position.Coordinate.Point;
                    // ReSharper disable AccessToDisposedClosure
                    waiter.Set();
                    // ReSharper restore AccessToDisposedClosure
                };

                locater.PositionChanged += handler;

                try
                {
                    waiter.WaitOne(TimeSpan.FromSeconds(30));
                }
                finally
                {
                    locater.PositionChanged -= handler;
                }
            }

            return result;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="latitude">The latitude in decimal format.</param>
        /// <param name="longitude">The longitude in decimal format.</param>
        public DmsLocation(double latitude, double longitude)
        {
            _latitude = ExtractDmsPoint(latitude, PointType.Lat);
            _longitude = ExtractDmsPoint(longitude, PointType.Lon);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get the city/state etc at a specified geo point.
        /// </summary>
        /// <param name="point">The point to reverse geocode.</param>
        /// <returns>The address if found, otherwise null.</returns>
        public static IAsyncOperation<string> GetLocationName(Geopoint point)
        {
            return GetLocationNamePrivate(point).AsAsyncOperation();
        }

        /// <summary>
        /// Async task to get the current location.
        /// </summary>
        /// <returns></returns>
        public static Geopoint GetLocation()
        {
            return GetLocationPrivate();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The dms latitude.
        /// </summary>
        public DmsPoint Latitude
        {
            get { return _latitude; }
        }

        /// <summary>
        /// The dms longitude.
        /// </summary>
        public DmsPoint Longitude
        {
            get { return _longitude; }
        }

        #endregion
    }
}
