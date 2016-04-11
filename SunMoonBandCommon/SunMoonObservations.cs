/*
 *  Copyright © 2015 Russell Libby
 */
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SunMoonBandCommon
{
    /// <summary>
    /// Class for managing observation data for sun and moon.
    /// </summary>
    public sealed class SunMoonObservations
    {
        #region Private fields

        #endregion

        #region Private methods

        /// <summary>
        /// Parses the sunrise element.
        /// </summary>
        /// <param name="list">The list to add the observation to.</param>
        /// <param name="data">The data stream from the web response.</param>
        private static void ParseSunrise(ICollection<SunMoonObservation> list, string data)
        {
            var sunrise = Regex.Matches(data, "(<tr><td>Sunrise</td><td>)(.*)(</td></tr>)");

            if (sunrise.Count >= 1)
            {
                list.Add(new SunMoonObservation(0, "Sunrise", sunrise[0].Groups[2].Value));
            }
        }

        /// <summary>
        /// Parses the sunset element.
        /// </summary>
        /// <param name="list">The list to add the observation to.</param>
        /// <param name="data">The data stream from the web response.</param>
        private static void ParseSunset(ICollection<SunMoonObservation> list, string data)
        {
            var sunset = Regex.Matches(data, "(<tr><td>Sunset</td><td>)(.*)(</td></tr>)");

            if (sunset.Count >= 1)
            {
                list.Add(new SunMoonObservation(1, "Sunset", sunset[0].Groups[2].Value));
            }
        }

        /// <summary>
        /// Parses the moonrise element.
        /// </summary>
        /// <param name="list">The list to add the observation to.</param>
        /// <param name="data">The data stream from the web response.</param>
        private static void ParseMoonrise(ICollection<SunMoonObservation> list, string data)
        {
            var moonrise = Regex.Matches(data, "(<tr><td>Moonrise</td><td>)(.*)(</td></tr>)");

            if (moonrise.Count >= 1)
            {
                list.Add(new SunMoonObservation(2, "Moonrise", moonrise[moonrise.Count - 1].Groups[2].Value));
            }
        }

        /// <summary>
        /// Parses the moonset element.
        /// </summary>
        /// <param name="list">The list to add the observation to.</param>
        /// <param name="data">The data stream from the web response.</param>
        private static void ParseMoonset(ICollection<SunMoonObservation> list, string data)
        {
            var moonset = Regex.Matches(data, "(<tr><td>Moonset</td><td>)(.*)(</td></tr>)");

            if (moonset.Count >= 1)
            {
                list.Add(new SunMoonObservation(3, "Moonset", moonset[0].Groups[2].Value));
            }
        }

        /// <summary>
        /// Parses the moon phase element.
        /// </summary>
        /// <param name="list">The list to add the observation to.</param>
        /// <param name="data">The data stream from the web response.</param>
        private static void ParseMoonphase(ICollection<SunMoonObservation> list, string data)
        {
            var pos = data.LastIndexOf("</td></tr></table>", StringComparison.OrdinalIgnoreCase);

            if (pos < 1) return;

            data = data.Substring(pos).Replace('\n', ' ');

            var moonphase = Regex.Matches(data, "<p>(.+?)</p>");
            var info = String.Empty;
            var phase = String.Empty;

            for (var i = 0; i < moonphase.Count; i++)
            {
                if (i == 0) info = moonphase[i].Groups[1].Value;
                if (i == 1) phase = moonphase[i].Groups[1].Value;
            }

            if (String.IsNullOrEmpty(info)) return;

            var observation = new SunMoonObservation(4, "Moon", String.Empty);
            var parts = info.Split(new[] {": "}, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length != 2) return;

            ParseMoonphaseNameIcon(observation, parts[1]);

            var match = Regex.Matches(phase, ": (.+?) with (.+?)%");

            if (match.Count > 0)
            {
                observation.SecondaryTitle = match[0].Groups[1].Value.Trim();
                observation.Content = String.Format("{0}%", match[0].Groups[2].Value);
            }

            list.Add(observation);
        }

        /// <summary>
        /// Parses the moon phase name and icon.
        /// </summary>
        /// <param name="item">The observation to update.</param>
        /// <param name="data">The data stream from the web response.</param>
        private static void ParseMoonphaseNameIcon(SunMoonObservation item, string data)
        {
            if ((item == null) || (String.IsNullOrEmpty(data))) return;

            var pos = data.IndexOf(' ');

            if (pos < 1) return;

            var moon = data.Substring(0, pos);

            if (moon.Equals("New", StringComparison.OrdinalIgnoreCase))
            {
                item.Content = "0%";
                item.SecondaryTitle = "New Moon";
                item.IconIndex = 4;
                return;
            }

            if (moon.Equals("First", StringComparison.OrdinalIgnoreCase))
            {
                item.Content = "50%";
                item.SecondaryTitle = "First Quarter";
                item.IconIndex = 5;
                return;
            }

            if (moon.Equals("Full", StringComparison.OrdinalIgnoreCase))
            {
                item.Content = "100%";
                item.SecondaryTitle = "Full Moon";
                item.IconIndex = 6;
                return;
            }

            if (!moon.Equals("Last", StringComparison.OrdinalIgnoreCase)) return;

            item.Content = "50%";
            item.SecondaryTitle = "Last Quarter";
            item.IconIndex = 7;
        }

        /// <summary>
        /// Async task to get the observation data from the military website
        /// </summary>
        /// <param name="latitude">The latitude for the user's current location.</param>
        /// <param name="longitude">The longitude for the user's current location.</param>
        /// <returns>The list of observations; sunrise, sunset, moonrise, moonset, and moon phase.</returns>
        private static async Task<IList<SunMoonObservation>> GetObservationDataPrivate(double latitude, double longitude)
        {
            var result = new List<SunMoonObservation>();
            var dms = new DmsLocation(latitude, longitude);
            var tzoffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            var client = new HttpClient();
            
            var address = String.Format(
                                            Common.ObservationUri, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 
                                            dms.Longitude.Sign, dms.Longitude.Degrees, dms.Longitude.Minutes,
                                            dms.Latitude.Sign, dms.Latitude.Degrees, dms.Latitude.Minutes, 
                                            Math.Abs(tzoffset.Hours), tzoffset.Hours < 0 ? (-1) : 1
                                        );

            try
            {
                using (var response = await client.GetAsync(address))
                {
                    var data = await response.Content.ReadAsStringAsync();

                    ParseSunrise(result, data);
                    ParseSunset(result, data);
                    ParseMoonphase(result, data);
                    ParseMoonrise(result, data);
                    ParseMoonset(result, data);
                }
            }
            catch
            {
                result.Clear();
            }

            return result;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Async task to get the observation data from the NOAA website.
        /// </summary>
        /// <param name="latitude">The latitude for the user's current location.</param>
        /// <param name="longitude">The longitude for the user's current location.</param>
        /// <returns>The list of observations; sunrise, sunset, moonrise, moonset, and moon phase.</returns>
        public static IAsyncOperation<IList<SunMoonObservation>> GetObservationData(double latitude, double longitude)
        {
            return GetObservationDataPrivate(latitude, longitude).AsAsyncOperation();
        }

        #endregion
    }
}
