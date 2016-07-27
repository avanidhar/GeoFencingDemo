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
        // Provides access to location data
        private Geofence fence = null;
        private CoreWindow coreWindow;
        public MainPage()
        {
            this.InitializeComponent();
            this.InitializeGeolocation();

            coreWindow = CoreWindow.GetForCurrentThread(); // this needs to be set before InitializeComponent sets up event registration for app visibility
            coreWindow.VisibilityChanged += OnVisibilityChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

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
            GeofenceMonitor.Current.GeofenceStateChanged -= OnGeofenceStateChanged;
            GeofenceMonitor.Current.StatusChanged -= OnGeofenceStatusChanged;

            base.OnNavigatingFrom(e);
        }

        private void OnVisibilityChanged(CoreWindow sender, VisibilityChangedEventArgs args)
        {
            // NOTE: After the app is no longer visible on the screen and before the app is suspended
            // you might want your app to use toast notification for any geofence activity.
            // By registering for VisibiltyChanged the app is notified when the app is no longer visible in the foreground.

            if (args.Visible)
            {
                // register for foreground events
                GeofenceMonitor.Current.GeofenceStateChanged += OnGeofenceStateChanged;
                GeofenceMonitor.Current.StatusChanged += OnGeofenceStatusChanged;
            }
            else
            {
                // unregister foreground events (let background capture events)
                GeofenceMonitor.Current.GeofenceStateChanged -= OnGeofenceStateChanged;
                GeofenceMonitor.Current.StatusChanged -= OnGeofenceStatusChanged;
            }
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        public async void NotifyUser(string strMessage, NotifyType type)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
            });
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
                        this.NotifyUser("Fence REMOVED", NotifyType.StatusMessage);
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
