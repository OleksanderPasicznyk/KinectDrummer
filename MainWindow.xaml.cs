namespace KinectDrummer
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using Microsoft.Kinect;
    using System.Linq;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel;
        private KinectSensor sensor;
        //private byte[] pixelData;
        private WriteableBitmap writeableBMP;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainWindowViewModel();
            this.DataContext = this.viewModel;
            this.Loaded += MainWindow_Loaded;
        }

        protected void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitializeKinect();
        }

        protected void DepthFrameHandler(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    short[] pixelData = new short[depthFrame.PixelDataLength];
                    int stride = depthFrame.Width * depthFrame.BytesPerPixel;
                    depthFrame.CopyPixelDataTo(pixelData);
                    this.writeableBMP.WritePixels(
                        new Int32Rect(0, 0, this.writeableBMP.PixelWidth,
                            this.writeableBMP.PixelHeight),
                        pixelData, stride, 0);
                }
            }
        }

        protected void ColorFrameHandler(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame != null)
                {
                    byte[] pixelData = new byte[imageFrame.PixelDataLength];
                    imageFrame.CopyPixelDataTo(pixelData);
                    int stride = imageFrame.Width * imageFrame.BytesPerPixel;
                    this.writeableBMP.WritePixels(
                        new Int32Rect(0, 0, this.writeableBMP.PixelWidth,
                            this.writeableBMP.PixelHeight),
                        pixelData, stride, 0);
                }
            }
        }

        private void ClickedStart(object sender, RoutedEventArgs e)
        {
            this.StartKinect();
        }

        private void ClickedStop(object sender, RoutedEventArgs e)
        {
            this.StopKinect();
        }

        private void SlidAngleSlider(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.SetSensorAngle(Int32.Parse(e.NewValue.ToString()));
        }

        private void TrackPlayer(short[] depthFrame)
        {
            //to be implemented on next commit
        }

        private void InitializeKinect()
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                this.sensor = KinectSensor.KinectSensors.FirstOrDefault(
                    sensorItem => sensorItem.Status == KinectStatus.Connected);
                this.StartKinect();
                //this conditional may not be good -- the last enable takes precedence
                //and we want the default (640x480rgb) so if something else enables
                //first, then the default will not get called.  it may not be worth the time
                //to also add a check for the type of stream...it may be better just to
                //be a steam roller
                if (!this.sensor.DepthStream.IsEnabled)
                {
                    this.sensor.DepthStream.Enable();
                }
                this.writeableBMP = new WriteableBitmap(
                    this.sensor.ColorStream.FrameWidth,
                    this.sensor.ColorStream.FrameHeight,
                    96, 96, PixelFormats.Gray16, null);
                this.KinectVideoStream.Source = this.writeableBMP;
                this.sensor.DepthFrameReady += this.DepthFrameHandler;
            }
            else
            {
                //notify the user that no Kinect is connected, allow them to retry
                MessageBox.Show("No Kinect found connected to the computer.");
                this.Close();
            }
        }

        private void StartKinect()
        {
            if (this.sensor != null && !this.sensor.IsRunning)
            {
                this.sensor.Start();
                this.viewModel.CanStart = false;
                this.viewModel.CanStop = true;
            }
        }

        private void StopKinect()
        {
            if (this.sensor != null && this.sensor.IsRunning)
            {
                this.sensor.Stop();
                this.viewModel.CanStart = true;
                this.viewModel.CanStop = false;
            }
        }

        private void SetSensorAngle(int angleValue)
        {
            if (angleValue > sensor.MinElevationAngle && angleValue < sensor.MaxElevationAngle)
            {
                this.sensor.ElevationAngle = angleValue;
            }
        }
    }
}
