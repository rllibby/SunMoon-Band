/*
 *  Copyright © 2015 Russell Libby
 */
using System;
using Windows.ApplicationModel.Email;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SunMoonBandCommon;

namespace SunMoonBand.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage
    {
        #region Private methods

        /// <summary>
        /// Event that is triggered when the hyperlink is clicked.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private async void EmailClicked(object sender, RoutedEventArgs e)
        {
            var sendTo = new EmailRecipient
            {
                Address = Common.Email
            };

            var mail = new EmailMessage {Subject = String.Format("{0} {1}", Common.Title, Common.Version), Body = String.Empty};

            mail.To.Add(sendTo);

            await EmailManager.ShowComposeNewEmailAsync(mail);
        }

        /// <summary>
        /// Handle the back key press.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The back key press event arguments.</param>
        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            var rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null || !rootFrame.CanGoBack) return;

            rootFrame.GoBack();
            e.Handled = true;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Event that is triggered when the page is navigated to.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = this;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public AboutPage()
        {
            InitializeComponent();

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Returns the version number.
        /// </summary>
        public string Version
        {
            get { return String.Format("version {0}", Common.Version); }
        }

        /// <summary>
        /// Returns the email address.
        /// </summary>
        public string Email
        {
            get { return Common.Email; }
        }

        /// <summary>
        /// Returns the last sync string.
        /// </summary>
        public string LastSync
        {
            get
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                var lastSync = localSettings.Values[Common.LastSyncKey] as String;

                return String.Format("Last band sync: {0}", String.IsNullOrEmpty(lastSync) ? "never" : lastSync);
            }
        }

        #endregion
    }
}