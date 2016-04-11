/*
 *  Copyright © 2015 Russell Libby
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml.Media.Imaging;

namespace SunMoonBandCommon
{
    /// <summary>
    /// Class to maintain a single observation.
    /// </summary>
    public sealed class SunMoonObservation : INotifyPropertyChanged
    {
        #region Private fields

        private static readonly List<Uri> Images = new List<Uri>
        {
            new Uri("ms-appx:///Assets/sunrise.png"),
            new Uri("ms-appx:///Assets/sunset.png"),
            new Uri("ms-appx:///Assets/moonrise.png"),
            new Uri("ms-appx:///Assets/moonset.png"),
            new Uri("ms-appx:///Assets/1moon.png"),
            new Uri("ms-appx:///Assets/2moon.png"),
            new Uri("ms-appx:///Assets/3moon.png"),
            new Uri("ms-appx:///Assets/4moon.png")
        };

        private string _title;
        private string _secondaryTitle;
        private string _content;
        private int _icon;

        #endregion

        #region Private methods

        /// <summary>
        /// Invoked when a property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        private void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;

            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Parses the content into discrete parts.
        /// </summary>
        /// <param name="content">The content to parse.</param>
        private void ParseContent(string content)
        {
            var extra = String.Empty;

            var index = content.IndexOf(" on preceding day", StringComparison.OrdinalIgnoreCase);

            if (index > 0)
            {
                extra = " •";
                content = content.Remove(index);
            }

            var pos = content.IndexOf(':');

            if ((pos < 1) || (_icon > 3))
            {
                _content = content;
                return;
            }

            var hours = content.Substring(0, pos);
            int value;

            if (Int32.TryParse(hours, out value))
            {
                if (value >= 12)
                {
                    if (value > 12) value -= 12;

                    _secondaryTitle = "PM" + extra;
                    _content = String.Format("{0:00}{1}", value, content.Substring(pos));

                    return;
                }
                
                if (value == 0)
                {
                    _secondaryTitle = "AM" + extra;
                    _content = String.Format("{0:00}{1}", 12, content.Substring(pos));

                    return;
                }
            }

            _content = content;
            _secondaryTitle = "AM" + extra;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="icon">The icon index to use for display on the band.</param>
        /// <param name="title">The title for the band page.</param>
        /// <param name="content">The content for the band page.</param>
        public SunMoonObservation(int icon, string title, string content)
        {
            _icon = icon;
            _title = title;
            _secondaryTitle = String.Empty;
            _content = String.Empty;
            
            ParseContent(content);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Returns the icon index for the observation.
        /// </summary>
        public int IconIndex
        {
            get { return _icon; }
            set
            {
                if (_icon == value) return;

                _icon = value;

                OnPropertyChanged("IconIndex");
                OnPropertyChanged("IconUri");
                OnPropertyChanged("IconSource");
            }
        }

        /// <summary>
        /// Returns the title for the observation.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;

                OnPropertyChanged("Title");
            }
        }

        /// <summary>
        /// Returns the title for the observation.
        /// </summary>
        public string SecondaryTitle
        {
            get { return _secondaryTitle; }
            set
            {
                _secondaryTitle = value;

                OnPropertyChanged("SecondaryTitle");
            }
        }

        /// <summary>
        /// Returns the content for the observation.
        /// </summary>
        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;

                OnPropertyChanged("Content");
            }
        }

        /// <summary>
        /// Returns the icon uri for the observation.
        /// </summary>
        public Uri IconUri
        {
            get { return Images[_icon]; }
        }

        /// <summary>
        /// Return the bitmap image for the icon.
        /// </summary>
        public BitmapImage IconSource
        {
            get { return new BitmapImage(IconUri); }
        }

        /// <summary>
        /// Event handler for property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
