/*
 *  Copyright © 2015 Russell Libby
 */

using Windows.Storage;

namespace SunMoonBandCommon
{
    /// <summary>
    /// Common constants shared between the UI and tasks runtime component.
    /// </summary>
    public static class Common
    {
        #region Public properties

        /// <summary>
        /// The application title.
        /// </summary>
        public static string Title
        {
            get { return @"SunMoon Band"; }
        }

        /// <summary>
        /// The application version.
        /// </summary>
        public static string Version
        {
            get { return "1.5.0.0"; }
        }

        /// <summary>
        /// The authors email address.
        /// </summary>
        public static string Email
        {
            get { return "rllibby@gmail.com"; }
        }

        /// <summary>
        /// The last update title.
        /// </summary>
        public static string LastUpdate
        {
            get { return "Last Update"; }
        }
        /// <summary>
        /// The date format used by application.
        /// </summary>
        public static string DateFormat
        {
            get { return "MM/dd h:mm tt"; }
        }

        /// <summary>
        /// The date format used by the band page title.
        /// </summary>
        public static string DateTitleFormat
        {
            get { return "MMM dd"; }
        }
        
        /// <summary>
        /// Guid id for the band tile.
        /// </summary>
        public static string TileGuid
        {
            get { return "4D2D21D8-9FD2-432D-A321-E87DACC54594"; }
        }

        /// <summary>
        /// Name for mutex used for foreground and background sync.
        /// </summary>
        public static string MutexName
        {
            get { return "SunMoonBandSync"; }
        }

        /// <summary>
        /// The background timer task name.
        /// </summary>
        public static string TimerTaskName
        {
            get { return "SunMoonBandTimerTask"; }
        }

        /// <summary>
        /// The background system task name.
        /// </summary>
        public static string SystemTaskName
        {
            get { return "SunMoonBandSystemTask"; }
        }

        /// <summary>
        /// The background device connection task name.
        /// </summary>
        public static string DeviceTaskName
        {
            get { return "SunMoonBandDeviceTask"; }
        }

        /// <summary>
        /// Setting key for last sync data.
        /// </summary>
        public static string LastSyncKey
        {
            get { return "lastsync"; }
        }

        /// <summary>
        /// Setting key for application data.
        /// </summary>
        public static string SettingKey
        {
            get { return "lastlocation"; }
        }
        
        /// <summary>
        /// Element id for title.
        /// </summary>
        public static short TitleId
        {
            get { return 1; }
        }

        /// <summary>
        /// Element id for spacer.
        /// </summary>
        public static short SpacerId
        {
            get { return 2; }
        }

        /// <summary>
        /// Element id for ssecondary title.
        /// </summary>
        public static short SeondaryTitleId
        {
            get { return 3; }
        }

        /// <summary>
        /// Element id for icon.
        /// </summary>
        public static short IconId
        {
            get { return 4; }
        }

        /// <summary>
        /// Element id for content.
        /// </summary>
        public static short ContentId
        {
            get { return 5; }
        }

        /// <summary>
        /// Element id for updated content.
        /// </summary>
        public static short UpdateId
        {
            get { return 6; }
        }

        /// <summary>
        /// Returns the link to the military observation data. 
        /// </summary>
        public static string ObservationUri
        {
            get
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                var useAzure = (localSettings.Values["UseAlternateSource"] != null) && (bool)localSettings.Values["UseAlternateSource"];

                return useAzure ? @"http://sunmoonbandazure.azurewebsites.net/api/data?form=2&ID=AA&year={0}&month={1}&day={2}&place=&lon_sign={3}&lon_deg={4}&lon_min={5}&lat_sign={6}&lat_deg={7}&lat_min={8}&tz={9}&tz_sign={10}" : @"http://aa.usno.navy.mil/rstt/onedaytable?form=2&ID=AA&year={0}&month={1}&day={2}&place=&lon_sign={3}&lon_deg={4}&lon_min={5}&lat_sign={6}&lat_deg={7}&lat_min={8}&tz={9}&tz_sign={10}";
            }    
        }

        #endregion
    }
}
