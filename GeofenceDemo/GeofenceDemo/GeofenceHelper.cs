using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GeofenceDemo
{
    public sealed partial class MainPage : Page
    {
        private CancellationTokenSource _cts = null;
        private static readonly string fenceId = "dummyFence";
        private async void GenerateGeofence()
        {
            bool fenceAlreadyExists = GeofenceMonitor.Current.Geofences.Any(g => g.Id == fenceId);

            if (!fenceAlreadyExists)
            {
                // Get cancellation token
                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;

                var geolocator = new Geolocator();

                // Request a high accuracy position for better accuracy locating the geofence
                geolocator.DesiredAccuracy = PositionAccuracy.High;
                Geoposition pos = await geolocator.GetGeopositionAsync().AsTask(token);

                BasicGeoposition position;
                position.Latitude = pos.Coordinate.Latitude;
                position.Longitude = pos.Coordinate.Longitude;
                position.Altitude = 0.0;
                double radius = 1;

                // the geofence is a circular region
                Geocircle geocircle = new Geocircle(position, radius);

                bool singleUse = false;

                // want to listen for enter geofence, exit geofence and remove geofence events
                // you can select a subset of these event states
                MonitoredGeofenceStates mask = MonitoredGeofenceStates.Entered | MonitoredGeofenceStates.Exited | MonitoredGeofenceStates.Removed;

                TimeSpan dwellTime;
                TimeSpan duration;
                DateTimeOffset startTime;

                try
                {
                    dwellTime = TimeSpan.FromSeconds(10);
                    duration = TimeSpan.FromSeconds(20);
                    startTime = DateTime.Now;
                }
                catch (ArgumentNullException)
                {
                }

                fence = new Geofence(fenceId, geocircle, mask, singleUse, dwellTime, startTime, duration);

                GeofenceMonitor.Current.Geofences.Add(fence);
                this.NotifyUser("GeoFence succesfully created", NotifyType.StatusMessage);
            }
            else
            {
                var existingFence = GeofenceMonitor.Current.Geofences.Where(g => g.Id == fenceId).FirstOrDefault();
                this.NotifyUser("Fence already exists. Using it", NotifyType.StatusMessage);
            }

    }



    /// <summary>
    /// This is the click handler for the 'Create Geofence' button.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnCreateGeofence(object sender, RoutedEventArgs e)
    {
        try
        {
            // get lat/long/radius, the fence name (fenceKey), 
            // and other properties from controls,
            // depending on data in controls for activation time
            // and duration the appropriate
            // constructor will be used.
            GenerateGeofence();

        }
        catch (TaskCanceledException)
        {
            this.NotifyUser("Canceled", NotifyType.StatusMessage);
        }
        catch (Exception ex)
        {
            // GeofenceMonitor failed in adding a geofence
            // exceptions could be from out of memory, lat/long out of range,
            // too long a name, not a unique name, specifying an activation
            // time + duration that is still in the past
            this.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
        }
    }
}
}
