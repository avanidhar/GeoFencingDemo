using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace GeofenceDemo
{
    public sealed partial class MainPage : Page
    {
        private async void InitializeGeolocation()
        {
            // Get permission to use location
            var accessStatus = await Geolocator.RequestAccessAsync();
            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                   
                    // register for state change events
                    GeofenceMonitor.Current.GeofenceStateChanged += OnGeofenceStateChanged;
                    GeofenceMonitor.Current.StatusChanged += OnGeofenceStatusChanged;
                    break;

                case GeolocationAccessStatus.Denied:
                    this.NotifyUser("Access denied.", NotifyType.ErrorMessage);
                    break;

                case GeolocationAccessStatus.Unspecified:
                    this.NotifyUser("Unspecified error.", NotifyType.ErrorMessage);
                    break;
            }
        }
        private Geofence GenerateGeofence()
        {
            string fenceKey = "simpleFence";

            BasicGeoposition position;
            position.Latitude = 47.6785619;
            position.Longitude = -122.1311156;
            position.Altitude = 0.0;
            double radius = 10;

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
                dwellTime = TimeSpan.FromSeconds(5);

                duration = TimeSpan.FromHours(1);

                startTime = DateTimeOffset.Now;
            }
            catch (ArgumentNullException)
            {
            }
            catch (FormatException)
            {
                this.NotifyUser("Entered value is not a valid string representation of a date and time", NotifyType.ErrorMessage);
            }
            catch (ArgumentException)
            {
                this.NotifyUser("The offset is greater than 14 hours or less than -14 hours.", NotifyType.ErrorMessage);
            }

            var geoFence = new Geofence(fenceKey, geocircle, mask, singleUse, dwellTime, startTime, duration);
            this.NotifyUser("GeoFence succesfully created", NotifyType.StatusMessage);

            return geoFence;
        }

        public async void OnGeofenceStateChanged(GeofenceMonitor sender, object e)
        {
            var reports = sender.ReadReports();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (GeofenceStateChangeReport report in reports)
                {
                    GeofenceState state = report.NewState;

                    Geofence geofence = report.Geofence;

                    if (state == GeofenceState.Removed)
                    {
                        // remove the geofence from the geofences collection
                        GeofenceMonitor.Current.Geofences.Remove(geofence);
                    }
                    else if (state == GeofenceState.Entered)
                    {
                        // Your app takes action based on the entered event

                        // NOTE: You might want to write your app to take particular
                        // action based on whether the app has internet connectivity.
                        this.NotifyUser("Fence ENTERED", NotifyType.StatusMessage);

                    }
                    else if (state == GeofenceState.Exited)
                    {
                        // Your app takes action based on the exited event

                        // NOTE: You might want to write your app to take particular
                        // action based on whether the app has internet connectivity.
                        this.NotifyUser("Fence EXITED", NotifyType.StatusMessage);

                    }
                }
            });
        }

        public async void OnGeofenceStatusChanged(GeofenceMonitor sender, object e)
        {
            var status = sender.Status;

            this.NotifyUser("Fence status has changed to : " + status, NotifyType.StatusMessage);
        }

    }
}
