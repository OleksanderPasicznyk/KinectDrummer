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
        //private byte[] depth32;
        private DepthImagePixel[] depthImagePixels;
        private Skeleton[] allSkeletons;

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
                    /*
                    short[] pixelData = new short[depthFrame.PixelDataLength];
                    int stride = depthFrame.Width * depthFrame.BytesPerPixel * 2;
                    depthFrame.CopyPixelDataTo(pixelData);
                     */
                    /*
                    this.writeableBMP.WritePixels(
                        new Int32Rect(0, 0, this.writeableBMP.PixelWidth,
                            this.writeableBMP.PixelHeight),
                        pixelData, stride, 0);
                    */
                    depthFrame.CopyDepthImagePixelDataTo(this.depthImagePixels);
                    int stride = depthFrame.Width * depthFrame.BytesPerPixel;
                    //depth32 = new byte[depthFrame.PixelDataLength * 4];
                    //this.TrackPlayer(pixelData); //writes to depth32...this is implied
                    //and I don't like that :/
                    this.writeableBMP.WritePixels(
                        new Int32Rect(0, 0, this.writeableBMP.PixelWidth,
                            this.writeableBMP.PixelHeight),
                            depthImagePixels.Select(x=>x.Depth).ToArray(), stride, 0);
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

        protected void SkeletonFrameHandler(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return;
                }
                skeletonFrame.CopySkeletonDataTo(allSkeletons);
                Skeleton firstSkeleton = (from trackedSkeleton in allSkeletons
                                          where trackedSkeleton.TrackingState
                                          == SkeletonTrackingState.Tracked
                                          select trackedSkeleton).FirstOrDefault();
                if (firstSkeleton == null)
                {
                    return;
                }
                if (firstSkeleton.Joints[JointType.HandRight].TrackingState ==
                    JointTrackingState.Tracked)
                {
                    this.MapJointsWithUIElement(firstSkeleton);
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
        /*
        private void TrackPlayer(short[] depthFrame)
        {
            for (int depthIndex = 0, colorIndex = 0;
                depthIndex < depthFrame.Length && colorIndex < this.depth32.Length;
                depthIndex++, colorIndex += 4)
            {
                int player = depthFrame[depthIndex] & DepthImageFrame.PlayerIndexBitmask;
                if (player > 0)
                {
                    depth32[colorIndex + 2] = 169;
                    depth32[colorIndex + 1] = 62;
                    depth32[colorIndex + 0] = 9;
                }
            }
        }
        */
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
                this.sensor.DepthStream.Range = DepthRange.Near;
                if (!this.sensor.SkeletonStream.IsEnabled)
                {
                    this.sensor.SkeletonStream.Enable();
                    //this.sensor.SkeletonStream.Enable(TransformSmoothParameters smoothParameters);
                }
                this.sensor.SkeletonStream.EnableTrackingInNearRange = true;
                this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                this.sensor.SkeletonFrameReady += SkeletonFrameHandler;
                allSkeletons = new Skeleton[this.sensor.SkeletonStream.FrameSkeletonArrayLength];
                /*
                depthImagePixels = new DepthImagePixel[
                    sensor.DepthStream.FramePixelDataLength];
                this.writeableBMP = new WriteableBitmap(
                    this.sensor.DepthStream.FrameWidth,
                    this.sensor.DepthStream.FrameHeight,
                    96, 96, PixelFormats.Gray16, null);
                this.KinectVideoStream.Source = this.writeableBMP;
                this.sensor.DepthFrameReady += this.DepthFrameHandler;
                 * */
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

        private void MapJointsWithUIElement(Skeleton skeleton)
        {
            Point mappedPoint = this.ScalePosition(skeleton.Joints[JointType.HandRight].Position);
            Canvas.SetLeft(rightHand, mappedPoint.X);
            Canvas.SetTop(rightHand, mappedPoint.Y);
        }

        private Point ScalePosition(SkeletonPoint skeletonPoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.
                MapSkeletonPointToDepthPoint(skeletonPoint,
                    DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }
    }
}
