/*
 *  Copyright © 2015 Russell Libby
 */
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using SunMoonBand.Theme;
using SunMoonBandCommon;
using Microsoft.Band;
using Microsoft.Band.Tiles;
using Microsoft.Band.Tiles.Pages;
using TextBlock = Microsoft.Band.Tiles.Pages.TextBlock;

#pragma warning disable 4014

namespace SunMoonBand.Pages
{
    /// <summary>
    /// The main page for the Buoy Band application.
    /// </summary>
    public sealed partial class MainPage
    {
        #region Private fields

        private static IBackgroundTaskRegistration _timerRegistration;
        private static IBackgroundTaskRegistration _systemRegistration;
        private static IBackgroundTaskRegistration _deviceRegistration;

        private App _viewModel;

        #endregion

        #region Private methods

        /// <summary>
        /// Async task to get the current location.
        /// </summary>
        /// <returns></returns>
        private static async Task<Geopoint> GetLocation()
        {
            var locater = new Geolocator
            {
                DesiredAccuracy = PositionAccuracy.Default,
                DesiredAccuracyInMeters = 1000
            };

            var position = await locater.GetGeopositionAsync(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(15));

            return position.Coordinate.Point;
        }

        /// <summary>
        /// Gets the first band instance found and returns the device connection trigger.
        /// </summary>
        /// <returns>The trigger if found, otherwise null.</returns>
        private static async Task<DeviceConnectionChangeTrigger> GetDeviceTrigger()
        {
            foreach (var di in await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(new Guid("C742E1A2-6320-5ABC-9643-D206C677E580")))))
            {
                try
                {
                    return await DeviceConnectionChangeTrigger.FromIdAsync(di.Id);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Unregisters our background task.
        /// </summary>
        private void UnregisterTask()
        {
            try
            {
                if (_timerRegistration != null)
                {
                    _timerRegistration.Completed -= OnTaskCompleted;
                    _timerRegistration.Progress -= OnTaskProgress;
                    _timerRegistration.Unregister(true);
                }

                if (_systemRegistration != null)
                {
                    _systemRegistration.Completed -= OnTaskCompleted;
                    _systemRegistration.Progress -= OnTaskProgress;
                    _systemRegistration.Unregister(true);
                }

                if (_deviceRegistration == null) return;

                _deviceRegistration.Completed -= OnTaskCompleted;
                _deviceRegistration.Progress -= OnTaskProgress;
                _deviceRegistration.Unregister(true);
            }
            finally
            {
                _timerRegistration = null;
                _systemRegistration = null;
                _deviceRegistration = null;
            }
        }

        /// <summary>
        /// Register the background task.
        /// </summary>
        /// <returns>True if we were able to register the background task.</returns>
        private async Task<bool> RegisterTask()
        {
            if (GetTaskRegistration()) return true;

            var access = await BackgroundExecutionManager.RequestAccessAsync().AsTask();

            if (access != BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity) return false;

            if (_timerRegistration == null)
            {
                var taskBuilder = new BackgroundTaskBuilder {Name = Common.TimerTaskName};
                var trigger = new TimeTrigger(41, false);

                taskBuilder.SetTrigger(trigger);
                taskBuilder.TaskEntryPoint = typeof (SunMoonBandTask.SunMoonBandTimerTask).FullName;
                taskBuilder.Register();
            }

            if (_systemRegistration == null)
            {
                var taskBuilder = new BackgroundTaskBuilder {Name = Common.SystemTaskName};
                var trigger = new SystemTrigger(SystemTriggerType.TimeZoneChange, false);

                taskBuilder.SetTrigger(trigger);
                taskBuilder.TaskEntryPoint = typeof (SunMoonBandTask.SunMoonBandSystemTask).FullName;
                taskBuilder.Register();
            }

            if (_deviceRegistration == null)
            {
                var taskBuilder = new BackgroundTaskBuilder { Name = Common.DeviceTaskName };
                var trigger = await GetDeviceTrigger();

                if (trigger == null) return GetTaskRegistration();

                taskBuilder.SetTrigger(trigger);
                taskBuilder.TaskEntryPoint = typeof(SunMoonBandTask.SunMoonBandDeviceTask).FullName;
                taskBuilder.Register();
            }

            return GetTaskRegistration();
        }

        /// <summary>
        /// Attempts to get the registered task.
        /// </summary>
        /// <returns>True if the task was aquired, otherwise false.</returns>
        private bool GetTaskRegistration()
        {
            if ((_timerRegistration != null) && (_systemRegistration != null) && (_deviceRegistration != null)) return true;

            foreach (var task in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (task.Name.Equals(Common.TimerTaskName)) _timerRegistration = task;
                if (task.Name.Equals(Common.SystemTaskName)) _systemRegistration = task;
                if (task.Name.Equals(Common.DeviceTaskName)) _deviceRegistration = task;
            }

            if (_timerRegistration != null)
            {
                _timerRegistration.Completed += OnTaskCompleted;
                _timerRegistration.Progress += OnTaskProgress;
            }

            if (_systemRegistration != null)
            {
                _systemRegistration.Completed += OnTaskCompleted;
                _systemRegistration.Progress += OnTaskProgress;
            }

            if (_deviceRegistration != null)
            {
                _deviceRegistration.Completed += OnTaskCompleted;
                _deviceRegistration.Progress += OnTaskProgress;
            }

            return ((_timerRegistration != null) && (_systemRegistration != null) && (_deviceRegistration != null));
        }

        /// <summary>
        /// Triggered when thhe background task is running and updating progress.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnTaskProgress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _viewModel.IsSyncing = true;
                _viewModel.BackgroundSync = true;
            });
        }

        /// <summary>
        /// Triggered when the background task has completed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnTaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (_viewModel.BackgroundSync) _viewModel.IsSyncing = false;
                _viewModel.BackgroundSync = false;
            });
        }

        /// <summary>
        /// Shows a message dialog.
        /// </summary>
        /// <param name="message">The dialog content to display.</param>
        /// <returns>The async task that can be awaited.</returns>
        private async Task ShowDialog(string message)
        {
            var dialog = new MessageDialog(message, Common.Title);

            dialog.Commands.Add(new UICommand(Dialogs.Ok, CommandWarning));

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Loads the png file from storage and creates a band tile icon from it.
        /// </summary>
        /// <param name="uri">The storage uri for the image.</param>
        /// <returns>The band tile icon.</returns>
        private static async Task<BandIcon> LoadIcon(string uri)
        {
            var imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

            using (var fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                var bitmap = new WriteableBitmap(1, 1);

                await bitmap.SetSourceAsync(fileStream);

                return bitmap.ToBandIcon();
            }
        }

        /// <summary>
        /// Command handlers for the warning dialog.
        /// </summary>
        /// <param name="commandLabel">The command selected by the user.</param>
        private void CommandWarning(IUICommand commandLabel)
        {
            Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Attempts to refresh / resync with the Microsoft band.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void AddTile(object sender, RoutedEventArgs e)
        {
            RunBandCheck();
        }

        /// <summary>
        /// Attempts to remove the application tile from the Microsoft band.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void RemoveTile(object sender, RoutedEventArgs e)
        {
            DeleteTile();
        }

        /// <summary>
        /// Generates the page layout for the first type of page.
        /// </summary>
        /// <returns>The page layout.</returns>
        private static PageLayout GeneratePageOne()
        {
            var titleBlock = new TextBlock
            {
                ColorSource = ElementColorSource.BandHighlight,
                ElementId = Common.TitleId,
                Rect = new PageRect(0, 0, 0, 35),
                AutoWidth = true,
                Baseline = 30,
                BaselineAlignment = TextBlockBaselineAlignment.Absolute
            };

            var spacerBlock = new TextBlock
            {
                Color = new BandColor(0x77, 0x77, 0x77),
                ElementId = Common.SpacerId,
                Rect = new PageRect(0, 0, 0, 35),
                Margins = new Margins(5, 0, 5, 0),
                AutoWidth = true,
                Baseline = 30,
                BaselineAlignment = TextBlockBaselineAlignment.Absolute
            };

            var secondaryTitleBlock = new TextBlock
            {
                ColorSource = ElementColorSource.BandSecondaryText,
                ElementId = Common.SeondaryTitleId,
                Rect = new PageRect(0, 0, 0, 35),
                AutoWidth = true,
                Baseline = 30,
                BaselineAlignment = TextBlockBaselineAlignment.Absolute
            };

            var topFlowPanel = new FlowPanel(titleBlock, spacerBlock, secondaryTitleBlock)
            {
               Rect = new PageRect(0, 0, 230, 40),
               Orientation = FlowPanelOrientation.Horizontal
            };

            var iconBlock = new Icon
            {
                ElementId = Common.IconId,
                Rect = new PageRect(0, 0, 46, 46),
                Margins = new Margins(0, 5, 10, 0),
                Color = new BandColor(0xff, 0xff, 0xff),
            };

            var contentBlock = new TextBlock
            {
                Color = new BandColor(0xff, 0xff, 0xff),
                Font = TextBlockFont.ExtraLargeNumbers,
                ElementId = Common.ContentId,
                Rect = new PageRect(0, 0, 0, 66),
                AutoWidth = true,
                Baseline = 91,
                BaselineAlignment = TextBlockBaselineAlignment.Absolute
            };

            var bottomFlowPanel = new FlowPanel(iconBlock, contentBlock)
            {
               Rect = new PageRect(0, 0, 230, 66),
               Orientation = FlowPanelOrientation.Horizontal
            };

            var panel = new FlowPanel(topFlowPanel, bottomFlowPanel)
            {
                Rect = new PageRect(15, 0, 230, 106)
            };

            return new PageLayout(panel);
        }

        /// <summary>
        /// Generates the page layout for the second type of page.
        /// </summary>
        /// <returns>The page layout.</returns>
        private static PageLayout GeneratePageTwo()
        {
            var updatedBlock = new WrappedTextBlock
            {
                ColorSource = ElementColorSource.BandSecondaryText,
                ElementId = Common.UpdateId,
                AutoHeight = true,
                Rect = new PageRect(0, 10, 200, 0),
            };

            var panel = new FlowPanel(updatedBlock)
            {
                Rect = new PageRect(15, 0, 230, 106)
            };

            return new PageLayout(panel);
        }

        /// <summary>
        /// Async method to remove the application tile from the Microsoft band.
        /// </summary>
        private async void DeleteTile()
        {
            string error = null;

            using (var sync = new AppSyncLock(Timeout.Infinite))
            {
                try
                {
                    if (!sync.IsLocked) return;

                    _viewModel.IsSyncing = true;

                    var localSettings = ApplicationData.Current.LocalSettings;

                    localSettings.Values[Common.LastSyncKey] = null;

                    UnregisterTask();

                    var pairedBands = await BandClientManager.Instance.GetBandsAsync();

                    if (pairedBands.Length < 1)
                    {
                        _viewModel.IsPaired = _viewModel.IsTileAdded = false;
                        await ShowDialog(Dialogs.NotPaired);

                        return;
                    }

                    _viewModel.IsPaired = true;

                    using (var bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                    {
                        var tiles = await bandClient.TileManager.GetTilesAsync();

                        if (tiles.Any()) await bandClient.TileManager.RemoveTileAsync(new Guid(Common.TileGuid));

                        _viewModel.IsTileAdded = false;
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    _viewModel.IsSyncing = false;
                }
            }

            if (string.IsNullOrEmpty(error)) return;

            await ShowDialog(error);
        }

        /// <summary>
        /// Add the additional tile icons to the band tile.
        /// </summary>
        /// <param name="tile">The band tile.</param>
        private static async void AddTileIcons(BandTile tile)
        {
            if (tile == null) return;

            tile.AdditionalIcons.Add(await LoadIcon("ms-appx:///Assets/sunrise.png"));
            tile.AdditionalIcons.Add(await LoadIcon("ms-appx:///Assets/sunset.png"));
            tile.AdditionalIcons.Add(await LoadIcon("ms-appx:///Assets/moonrise.png"));
            tile.AdditionalIcons.Add(await LoadIcon("ms-appx:///Assets/moonset.png"));
            tile.AdditionalIcons.Add(await LoadIcon("ms-appx:///Assets/1moon.png"));
            tile.AdditionalIcons.Add(await LoadIcon("ms-appx:///Assets/2moon.png"));
            tile.AdditionalIcons.Add(await LoadIcon("ms-appx:///Assets/3moon.png"));
            tile.AdditionalIcons.Add(await LoadIcon("ms-appx:///Assets/4moon.png"));
        }

        /// <summary>
        /// Ensures that a band is paired and attempts to add the tile if not already added.
        /// </summary>
        private async void RunBandCheck()
        {
            string error = null;

            using (var sync = new AppSyncLock(Timeout.Infinite))
            {
                try
                {
                    if (!sync.IsLocked) return;

                    _viewModel.IsSyncing = true;

                    await RegisterTask();

                    try
                    {
                        var point = await GetLocation();
                        var observations = await SunMoonObservations.GetObservationData(point.Position.Latitude, point.Position.Longitude);

                        _viewModel.LoadObservations(observations);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("permission")) throw new Exception(Dialogs.NoLocation);
                        if (ex.Message.Contains("timeout")) throw new Exception(Dialogs.Timeout);
                        throw;
                    }

                    var pairedBands = await BandClientManager.Instance.GetBandsAsync();

                    if (pairedBands.Length < 1)
                    {
                        _viewModel.IsPaired = _viewModel.IsTileAdded = false;
                        await ShowDialog(Dialogs.NotPaired);

                        return;
                    }

                    _viewModel.IsPaired = true;

                    using (var bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                    {
                        var tiles = await bandClient.TileManager.GetTilesAsync();

                        if (tiles.Any())
                        {
                            _viewModel.IsTileAdded = true;
                            return;
                        }

                        var tile = new BandTile(new Guid(Common.TileGuid))
                        {
                            Name = Common.Title,
                            TileIcon = await LoadIcon("ms-appx:///Assets/TileLarge.png"),
                            SmallIcon = await LoadIcon("ms-appx:///Assets/TileSmall.png"),
                        };

                        AddTileIcons(tile);

                        tile.PageLayouts.Add(GeneratePageOne());
                        tile.PageLayouts.Add(GeneratePageTwo());

                        _viewModel.IsPaired = true;

                        try
                        {
                            _viewModel.IsTileAdded = await bandClient.TileManager.AddTileAsync(tile);
                        }
                        catch (BandIOException bandex)
                        {
                            _viewModel.IsTileAdded = (bandex.Message.Contains("MissingManifestResource"));

                            if (!_viewModel.IsTileAdded) error = bandex.Message;
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    _viewModel.IsSyncing = false;
                }
            }

            if (string.IsNullOrEmpty(error)) return;

            ShowDialog(error);
        }

        /// <summary>
        /// Event that occurs when data is being synced to the band.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This routine attempts to aquire the mutex lock immediately, and exits if unable to do so. If it can't aquire the
        /// lock, it should mean that the background process is running a sync already.
        /// </remarks>
        private async void SyncClicked(object sender, RoutedEventArgs e)
        {
            using (var sync = new AppSyncLock(0))
            {
                if (!sync.IsLocked) return;

                string error;

                try
                {
                    _viewModel.IsSyncing = true;

                    var point = await GetLocation();
                    var observations = await SunMoonObservations.GetObservationData(point.Position.Latitude, point.Position.Longitude);
                    var address = await DmsLocation.GetLocationName(point);

                    _viewModel.LoadObservations(observations);

                    var pairedBands = await BandClientManager.Instance.GetBandsAsync();

                    if (pairedBands.Length < 1)
                    {
                        _viewModel.IsPaired = _viewModel.IsTileAdded = false;
                        await ShowDialog(Dialogs.NotPaired);

                        return;
                    }

                    _viewModel.IsPaired = true;

                    using (var bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                    {
                        var tiles = await bandClient.TileManager.GetTilesAsync();

                        if (!tiles.Any())
                        {
                            _viewModel.IsTileAdded = false;
                            await ShowDialog(Dialogs.TileRemoved);

                            return;
                        }

                        await bandClient.TileManager.RemovePagesAsync(new Guid(Common.TileGuid));

                        var pages = (from item in _viewModel.Observations 
                                     let title = new TextBlockData(Common.TitleId, item.Title) 
                                     let spacer = new TextBlockData(Common.SpacerId, "|") 
                                     let secondary = new TextBlockData(Common.SeondaryTitleId, item.SecondaryTitle) 
                                     let icon = new IconData(Common.IconId, (ushort) (item.IconIndex + 2)) 
                                     let content = new TextBlockData(Common.ContentId, item.Content) 
                                     select new PageData(Guid.NewGuid(), 0, title, spacer, secondary, icon, content)).ToList();

                        var description = String.Format("App Updated\n{0}\n{1}\n", DateTime.Now.ToString(Common.DateFormat), address);
                        var updated = new WrappedTextBlockData(Common.UpdateId, description);

                        pages.Add(new PageData(Guid.NewGuid(), 1, updated));

                        pages.Reverse();

                        await bandClient.TileManager.RemovePagesAsync(new Guid(Common.TileGuid));
                        await bandClient.TileManager.SetPagesAsync(new Guid(Common.TileGuid), pages);

                        var localSettings = ApplicationData.Current.LocalSettings;

                        localSettings.Values[Common.LastSyncKey] = DateTime.Now.ToString(Common.DateFormat);
                        error = (_viewModel.Observations.Count == 0) ? Dialogs.NoData : Dialogs.Synced;
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    _viewModel.IsSyncing = false;
                }

                if (String.IsNullOrEmpty(error)) return;

                await ShowDialog(error);
            }
        }

        /// <summary>
        /// Event that is fired when the about menu item is clicked.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void AboutClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.
        /// </param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");

            GetTaskRegistration();
            ThemeManager.SetThemeColor((Color)Application.Current.Resources["ThemeColor"]);

            DataContext = _viewModel = App.Current;

            if (e.NavigationMode == NavigationMode.New) Dispatcher.RunAsync(CoreDispatcherPriority.Normal, RunBandCheck);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constrructor.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
        }

        #endregion
    }
}
