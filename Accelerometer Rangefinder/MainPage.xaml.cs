// <copyright file="MainPage.xaml.cs" company="Jaden TOm">
//     Authored by Jaden Tom
// </copyright>

namespace Accelerometer_Rangefinder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading;
    using Windows.Devices.Enumeration;
    using Windows.Devices.Sensors;
    using Windows.Foundation;
    using Windows.Foundation.Collections;
    using Windows.Media;
    using Windows.Media.Capture;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Data;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// A synchronization context with which to update the UI thread
        /// </summary>
        private readonly SynchronizationContext syncContext;

        /// <summary>
        /// A boolean value indicating whether the distance to the device is set
        /// </summary>
        private bool isDistanceSet = false;

        /// <summary>
        /// The distance to the base of the object
        /// </summary>
        private double distance = 0;

        /// <summary>
        /// Timer which repeats and controls accelerator sampling
        /// </summary>
        private System.Threading.Timer accelTimer;

        /// <summary>
        /// The accelerometer which to read orientation values from
        /// </summary>
        private Accelerometer accelerometer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage" /> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            this.syncContext = SynchronizationContext.Current;
            this.BeginWebcamPreview();
            this.BeginAccelerometerPolling();
        }

        /// <summary>
        /// A method to start a webcam preview and display it
        /// </summary>
        private async void BeginWebcamPreview()
        {
            try
            {
                // Check if the machine has a webcam
                DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                if (devices.Count > 0)
                {
                    // Using Windows.Media.Capture.MediaCapture APIs to stream from webcam
                    MediaCapture mediaCaptureMgr = new MediaCapture();
                    await mediaCaptureMgr.InitializeAsync();

                    VideoStream.Source = mediaCaptureMgr;
                    await mediaCaptureMgr.StartPreviewAsync();
                }
                else
                {
                    AccelerometerText.Text = "Cannot run, no webcam detected";
                }
            }
            catch (Exception ex)
            {
                AccelerometerText.Text = "Exception in connecting to webcam: " + ex.Message;
            }
        }

        /// <summary>
        /// A method to start polling the accelerometer
        /// </summary>
        private void BeginAccelerometerPolling()
        {
            try
            {
                this.accelerometer = Accelerometer.GetDefault();
                if (this.accelerometer != null)
                {
                    this.accelTimer = new System.Threading.Timer(this.UpdateAccelerometerText, null, 100, 100);
                }
                else
                {
                    AccelerometerText.Text = "Cannot run, no accelerometer detected";
                }
            }
            catch (Exception ex)
            {
                AccelerometerText.Text = "Exception in connecting to accelerometer: " + ex.Message;
            }
        }

        /// <summary>
        /// Updates the accelerometer's values in the text block
        /// </summary>
        /// <param name="state">This parameter is unused.</param>
        private void UpdateAccelerometerText(object state)
        {
            AccelerometerReading reading = this.accelerometer.GetCurrentReading();
            if (reading != null)
            {
                this.syncContext.Post(
                    o =>
                    {
                        AccelerometerText.Text = "X: " + reading.AccelerationX
                            + "\nY: " + reading.AccelerationY
                            + "\nZ: " + reading.AccelerationZ;

                        double angle = Math.Acos(Math.Max(Math.Min(reading.AccelerationZ, 1), -1));
                        AngleText.Text = "Angle: " + angle;

                        double deviceHeight;
                        double.TryParse(DeviceHeightText.Text, out deviceHeight);

                        if (!this.isDistanceSet)
                        {
                            // Calculate distance based on device height and angle
                            this.distance = Math.Abs(Math.Tan(angle) * deviceHeight);

                            // deviceHeight and hence distance will be 0 if parsing fails, 0 is an invalid value anyways
                            if (this.distance == 0)
                            {
                                DistText.Text = "Please enter a valid device height";
                            }
                            else
                            {
                                DistText.Text = "Distance: " + this.distance;
                            }
                        }
                        else
                        {
                            //Calculate height based on recorded distance
                            double height = this.distance / Math.Tan(angle);
                            if (height == 0)
                            {
                                ObjHeightText.Text = "Please set a distance";
                            }
                            else
                            {
                                ObjHeightText.Text = "Height: " + (height + deviceHeight);
                            }
                        }
                    },
                    null);
            }
        }

        /// <summary>
        /// Changes the distance/height mode when the button is clicked.
        /// </summary>
        /// <param name="sender">This parameter is not used.</param>
        /// <param name="e">This parameter is not used.</param>
        private void DistSetButton_Click(object sender, RoutedEventArgs e)
        {
            this.isDistanceSet = !this.isDistanceSet;

            if (this.isDistanceSet)
            {
                DistSetButton.Content = "Unset Distance";
                DeviceHeightText.IsReadOnly = true;
            }
            else
            {
                DistSetButton.Content = "Set Distance";
                DeviceHeightText.IsReadOnly = false;
                ObjHeightText.Text = "Height:";
            }
        }
    }
}
