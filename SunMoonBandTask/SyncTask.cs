/*
 *  Copyright © 2015 Russell Libby
 */
using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Microsoft.Band;
using Microsoft.Band.Tiles.Pages;
using SunMoonBandCommon;

namespace SunMoonBandTask
{
    /// <summary>
    /// Common sync task routine to be called from all the different background tasks.
    /// </summary>
    internal static class SyncTask
    {
        #region Public methods

        /// <summary>
        /// Async method to run when the background task is executed.
        /// </summary>
        /// <param name="taskInstance">The background task instance being run.</param>
        public static async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                var isCancelled = false;

                using (var sync = new AppSyncLock(0))
                {
                    BackgroundTaskCanceledEventHandler cancelled = (sender, reason) =>
                    {
                        isCancelled = true;
                    };

                    try
                    {
                        if (!sync.IsLocked) return;

                        if (taskInstance.TriggerDetails is DeviceConnectionChangeTriggerDetails)
                        {
                            var deviceDetails = taskInstance.TriggerDetails as DeviceConnectionChangeTriggerDetails;
                            var device = await BluetoothDevice.FromIdAsync(deviceDetails.DeviceId);

                            if (device.ConnectionStatus != BluetoothConnectionStatus.Connected) return;
                        }

                        taskInstance.Progress = 1;

                        var point = DmsLocation.GetLocation();

                        taskInstance.Progress = 10;
                        if (point == null) throw new Exception("Timed out while attempting to determine location.");
                        if (isCancelled) return;

                        var address = await DmsLocation.GetLocationName(point);

                        taskInstance.Progress = 20;
                        if (isCancelled) return;

                        var observations = await SunMoonObservations.GetObservationData(point.Position.Latitude, point.Position.Longitude);

                        taskInstance.Progress = 30;
                        if (observations.Count == 0) throw new Exception("Failed to download observations.");
                        if (isCancelled) return;

                        var pairedBands = await BandClientManager.Instance.GetBandsAsync(true);

                        taskInstance.Progress = 40;
                        if ((pairedBands.Length < 1) || isCancelled) return;

                        using (var bandClient = await SmartConnect.ConnectAsync(pairedBands[0], 5, 2000))
                        {
                            taskInstance.Progress = 50;
                            if (isCancelled) return;

                            var tiles = await bandClient.TileManager.GetTilesAsync();

                            taskInstance.Progress = 60;
                            if (!tiles.Any() || isCancelled) return;

                            var pages = (from item in observations
                                         let title = new TextBlockData(Common.TitleId, item.Title)
                                         let spacer = new TextBlockData(Common.SpacerId, "|")
                                         let secondary = new TextBlockData(Common.SeondaryTitleId, item.SecondaryTitle)
                                         let icon = new IconData(Common.IconId, (ushort)(item.IconIndex + 2))
                                         let content = new TextBlockData(Common.ContentId, item.Content)
                                         select new PageData(Guid.NewGuid(), 0, title, spacer, secondary, icon, content)).ToList();

                            taskInstance.Progress = 70;
                            if (isCancelled) return;

                            var description = String.Format("Last Updated\n{0}\n{1}\n", DateTime.Now.ToString(Common.DateFormat), address);
                            var updated = new WrappedTextBlockData(Common.UpdateId, description);

                            pages.Add(new PageData(Guid.NewGuid(), 1, updated));
                            pages.Reverse();

                            await bandClient.TileManager.RemovePagesAsync(new Guid(Common.TileGuid));
                            taskInstance.Progress = 80;

                            await bandClient.TileManager.SetPagesAsync(new Guid(Common.TileGuid), pages);
                            taskInstance.Progress = 90;

                            localSettings.Values[Common.LastSyncKey] = DateTime.Now.ToString(Common.DateFormat);

                            taskInstance.Progress = 100;
                        }
                    }
                    catch (Exception ex)
                    {
                        localSettings.Values[Common.LastSyncKey] = String.Format("Failed at {0}\r\n{1}", DateTime.Now.ToString(Common.DateFormat), ex.Message);
                    }
                    finally
                    {
                        taskInstance.Canceled -= cancelled;
                        if (isCancelled) localSettings.Values[Common.LastSyncKey] = String.Format("Cancelled at {0}", DateTime.Now.ToString(Common.DateFormat));
                    }
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        #endregion
    }
}
