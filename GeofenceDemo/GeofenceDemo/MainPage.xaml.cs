using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GeofenceDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private uint accuracyInMeters = 1;
        
        // Proides access to location data
        private Geolocator _geolocator = null;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DesiredAccuracyInMeters.IsEnabled = true;
            DesiredAccuracyInMeters.Text = this.accuracyInMeters.ToString();
        }

        /// <summary>
        /// Invoked immediately before the Page is unloaded and is no longer the current source of a parent Frame.
        /// </summary>
        /// <param name="e">
        /// Event data that can be examined by overriding code. The event data is representative
        /// of the navigation that will unload the current Page unless canceled. The
        /// navigation can potentially be canceled by setting e.Cancel to true.
        /// </param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (StopSendingGeolocationButton.IsEnabled)
            {
                _geolocator.PositionChanged -= OnPositionChanged;
                _geolocator.StatusChanged -= OnStatusChanged;
            }

            base.OnNavigatingFrom(e);
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
        }

        async private void SendGeolocation(object sender, RoutedEventArgs e)
        {
            LocationDisabledMessage.Visibility = Visibility.Collapsed;
            try
            {
                // Request permission to access location
                var accessStatus = await Geolocator.RequestAccessAsync();

                switch (accessStatus)
                {
                    case GeolocationAccessStatus.Allowed:

                        var geoFences = GeofenceMonitor.Current.Geofences;
                        // You should set MovementThreshold for distance-based tracking
                        // or ReportInterval for periodic-based tracking before adding event
                        // handlers. If none is set, a ReportInterval of 1 second is used
                        // as a default and a position will be returned every 1 second.
                        //
                        // Value of 2000 milliseconds (2 seconds) 
                        // isn't a requirement, it is just an example.
                        _geolocator = new Geolocator { ReportInterval = 2000 };

                        // Subscribe to PositionChanged event to get updated tracking positions
                        _geolocator.PositionChanged += OnPositionChanged;

                        // Subscribe to StatusChanged event to get updates of location status changes
                        _geolocator.StatusChanged += OnStatusChanged;

                        this.NotifyUser("Waiting for update...", NotifyType.StatusMessage);
                        LocationDisabledMessage.Visibility = Visibility.Collapsed;
                        GetGeolocationButton.IsEnabled = false;
                        StopSendingGeolocationButton.IsEnabled = true;
                        break;

                    case GeolocationAccessStatus.Denied:
                        this.NotifyUser("Access to location is denied.", NotifyType.ErrorMessage);
                        LocationDisabledMessage.Visibility = Visibility.Visible;
                        UpdateLocationData(null);
                        break;

                    case GeolocationAccessStatus.Unspecified:
                        this.NotifyUser("Unspecified error.", NotifyType.ErrorMessage);
                        UpdateLocationData(null);
                        break;
                }
            }
            catch (TaskCanceledException)
            {
                this.NotifyUser("Canceled.", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                this.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
            }
            
            GetGeolocationButton.IsEnabled = false;
            StopSendingGeolocationButton.IsEnabled = true;
        }

        /// <summary>
        /// Event handler for PositionChanged events. It is raised when
        /// a location is available for the tracking session specified.
        /// </summary>
        /// <param name="sender">Geolocator instance</param>
        /// <param name="e">Position data</param>
        async private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.NotifyUser("Location updated.", NotifyType.StatusMessage);
                UpdateLocationData(e.Position);
            });
        }

        /// <summary>
        /// Event handler for StatusChanged events. It is raised when the 
        /// location status in the system changes.
        /// </summary>
        /// <param name="sender">Geolocator instance</param>
        /// <param name="e">Statu data</param>
        async private void OnStatusChanged(Geolocator sender, StatusChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Show the location setting message only if status is disabled.
                LocationDisabledMessage.Visibility = Visibility.Collapsed;

                switch (e.Status)
                {
                    case PositionStatus.Ready:
                        // Location platform is providing valid data.
                        ScenarioOutput_Status.Text = "Ready";
                        this.NotifyUser("Location platform is ready.", NotifyType.StatusMessage);
                        break;

                    case PositionStatus.Initializing:
                        // Location platform is attempting to acquire a fix. 
                        ScenarioOutput_Status.Text = "Initializing";
                        this.NotifyUser("Location platform is attempting to obtain a position.", NotifyType.StatusMessage);
                        break;

                    case PositionStatus.NoData:
                        // Location platform could not obtain location data.
                        ScenarioOutput_Status.Text = "No data";
                        this.NotifyUser("Not able to determine the location.", NotifyType.ErrorMessage);
                        break;

                    case PositionStatus.Disabled:
                        // The permission to access location data is denied by the user or other policies.
                        ScenarioOutput_Status.Text = "Disabled";
                        this.NotifyUser("Access to location is denied.", NotifyType.ErrorMessage);

                        // Show message to the user to go to location settings
                        LocationDisabledMessage.Visibility = Visibility.Visible;

                        // Clear cached location data if any
                        UpdateLocationData(null);
                        break;

                    case PositionStatus.NotInitialized:
                        // The location platform is not initialized. This indicates that the application 
                        // has not made a request for location data.
                        ScenarioOutput_Status.Text = "Not initialized";
                        this.NotifyUser("No request for location is made yet.", NotifyType.StatusMessage);
                        break;

                    case PositionStatus.NotAvailable:
                        // The location platform is not available on this version of the OS.
                        ScenarioOutput_Status.Text = "Not available";
                        this.NotifyUser("Location is not available on this version of the OS.", NotifyType.ErrorMessage);
                        break;

                    default:
                        ScenarioOutput_Status.Text = "Unknown";
                        this.NotifyUser(string.Empty, NotifyType.StatusMessage);
                        break;
                }
            });
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

                    }
                    else if (state == GeofenceState.Exited)
                    {
                        // Your app takes action based on the exited event

                        // NOTE: You might want to write your app to take particular
                        // action based on whether the app has internet connectivity.

                    }
                }
            });
        }

        /// <summary>
        /// Updates the user interface with the Geoposition data provided
        /// </summary>
        /// <param name="position">Geoposition to display its details</param>
        private void UpdateLocationData(Geoposition position)
        {
            if (position == null)
            {
                ScenarioOutput_Latitude.Text = "No data";
                ScenarioOutput_Longitude.Text = "No data";
                ScenarioOutput_Accuracy.Text = "No data";
            }
            else
            {
                ScenarioOutput_Latitude.Text = position.Coordinate.Point.Position.Latitude.ToString();
                ScenarioOutput_Longitude.Text = position.Coordinate.Point.Position.Longitude.ToString();
                ScenarioOutput_Accuracy.Text = position.Coordinate.Accuracy.ToString();
            }
        }

        private void StopSendingLocation(object sender, RoutedEventArgs e)
        {
            _geolocator.PositionChanged -= OnPositionChanged;
            _geolocator.StatusChanged -= OnStatusChanged;
            _geolocator = null;

            GetGeolocationButton.IsEnabled = true;
            StopSendingGeolocationButton.IsEnabled = false;

            // Clear status
            this.NotifyUser("", NotifyType.StatusMessage);
            UpdateLocationData(null);
        }
    }
}
