using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;

namespace KinectSDK18
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            sensor.ColorStream.Enable();
            sensor.SkeletonStream.Enable();
        }


        KinectSensor sensor = KinectSensor.KinectSensors[0];

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            sensor.SkeletonFrameReady += runtime_SkeletonFrameReady;
            sensor.ColorFrameReady += runtime_VideoFrameReady;
            sensor.Start();
            sensor.ElevationAngle = (int)sliderAngle.Value;
        }

        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            sensor.Stop();
        }

        void runtime_VideoFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame CFrame = e.OpenColorImageFrame())
            {
                if (CFrame != null)
                {
                    byte[] pixelData = new byte[CFrame.PixelDataLength];
                    CFrame.CopyPixelDataTo(pixelData);
                    BitmapSource source = BitmapSource.Create(640, 480, 96,96, PixelFormats.Bgr32, null, pixelData, 640 * 4);

                    videoImage.Source = source;
                }
            }
        }


        void runtime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame SFrame = e.OpenSkeletonFrame()){

            if (SFrame !=null)
            {
                Skeleton[] skeletons = new Skeleton[SFrame.SkeletonArrayLength];
                SFrame.CopySkeletonDataTo(skeletons);
                Skeleton currentSkeleton = (from s in skeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();

                if (currentSkeleton != null)
                {
                   ScalePosition(head, currentSkeleton.Joints[JointType.Head]);
                   ScalePosition(Lhand, currentSkeleton.Joints[JointType.HandLeft]);
                   ScalePosition(Rhand, currentSkeleton.Joints[JointType.HandRight]);
                }
            }
            }
        }
           
            
                private void ScalePosition(FrameworkElement element, Joint joint)
                {
                 //convert the value to X/Y
                Joint scaledJoint = joint.ScaleTo(640, 480); 

                   //convert & scale (.3 = means 1/3 of joint distance)
                //Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

                 Canvas.SetLeft(element, scaledJoint.Position.X);
                  Canvas.SetTop(element, scaledJoint.Position.Y); 

                }

                private void sliderAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
                {
                    labelAngel.Content = (int)sliderAngle.Value;
                }

                private void btnSetAngle_Click(object sender, RoutedEventArgs e)
                {
                    sensor.ElevationAngle = (int)sliderAngle.Value;
                }

        }




 }
