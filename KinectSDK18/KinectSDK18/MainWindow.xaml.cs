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
//Speech references
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.IO;


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
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
        }


        KinectSensor sensor = KinectSensor.KinectSensors[0];

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            sensor.SkeletonFrameReady += runtime_SkeletonFrameReady;
            sensor.ColorFrameReady += runtime_VideoFrameReady;
            sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(runtime_DepthFrameReady);
            sensor.Start();
            KinectAudio();
            sensor.ElevationAngle = (int)sliderAngle.Value;
        }

        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            sensor.Stop();
        }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////Method to get the RGB video from the kinect///////////////////////////////////////////////////////////////////////////////////////////////
        private byte[] pixelData;
        void runtime_VideoFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame CFrame = e.OpenColorImageFrame())
            {
                if (CFrame != null)
                {
                    this.pixelData = new byte[CFrame.PixelDataLength];
                    CFrame.CopyPixelDataTo(pixelData);
                    BitmapSource source = BitmapSource.Create(640, 480, 96,96, PixelFormats.Bgr32, null, pixelData, 640 * 4);

                    videoImage.Source = source;
                }
            }
        }
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////Function for getting head, left hand, right hand from skelton tracking///////////////////////////////////////////////////////////
        private Skeleton [] skeletons;
        void runtime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame SFrame = e.OpenSkeletonFrame())
            {
            if (SFrame !=null)
            {
                this.skeletons = new Skeleton[SFrame.SkeletonArrayLength];
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
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////Method to attach ellispses to hands using coding4fun wpf////////////////////////////////////////////////////////////    
                private void ScalePosition(FrameworkElement element, Joint joint)
                {
                 //convert the value to X/Y
                Joint scaledJoint = joint.ScaleTo(640, 480,1f,1f);// Needs adjusting 

                   //convert & scale (.3 = means 1/3 of joint distance)
               // Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

                 Canvas.SetLeft(element, scaledJoint.Position.X);
                  Canvas.SetTop(element, scaledJoint.Position.Y); 

                }
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////Methods to display Depth sensor Data/////////////////////////////////////////////////////////////////////////////////////////////////////

        private DepthImagePixel[] depthPixels;
        private byte[] DcolorPixels;
        private WriteableBitmap colorBitmap;

        void runtime_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame DepthFrame = e.OpenDepthImageFrame())
            {
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                    this.DcolorPixels = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                    this.colorBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth,
                    this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                    
                
                if (DepthFrame != null)
                {
                    DepthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    // Get the min and max reliable depth for the current frame
                    int minDepth = DepthFrame.MinDepth;
                    int maxDepth = DepthFrame.MaxDepth;

                    // Convert the depth to RGB
                    int colorPixelIndex = 0;

                    for (int i = 0; i < this.depthPixels.Length; ++i)
                    {
                        // Get the depth for this pixel
                        short depth = depthPixels[i].Depth;

                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                        // Write out blue byte
                        this.DcolorPixels[colorPixelIndex++] = intensity;

                        // Write out green byte
                        this.DcolorPixels[colorPixelIndex++] = intensity;

                        // Write out red byte                        
                        this.DcolorPixels[colorPixelIndex++] = intensity;

                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }

                    this.colorBitmap.WritePixels(
                     new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                     this.DcolorPixels,
                     this.colorBitmap.PixelWidth * sizeof(int),
                     0);

                    this.dImage.Source = this.colorBitmap;
                }

            }
        }




      
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////Methods for adjusting angle of kinect device through slider bar/////////////////////////////////////////////////////////////////////
                private void sliderAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
                {
                    labelAngel.Content = (int)sliderAngle.Value;
                }

                private void btnSetAngle_Click(object sender, RoutedEventArgs e)
                {
                    sensor.ElevationAngle = (int)sliderAngle.Value;
                }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////Code used for audio commands/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private SpeechRecognitionEngine speechEngine;

        private static RecognizerInfo GetKinectRecognizer()//Gets data for the speech recognizer(accoustic model) most suitable to process audio from kinect device.
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        private void KinectAudio()
        {

            RecognizerInfo ri = GetKinectRecognizer();

            this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                //Use this code to create grammar programmatically rather than from
                //a grammar file.
                 
                 var directions = new Choices();
                 directions.Add(new SemanticResultValue("red", "RED"));
                 directions.Add(new SemanticResultValue("blue", "BLUE"));
                 directions.Add(new SemanticResultValue("green", "GREEN"));
                 directions.Add(new SemanticResultValue("black", "BLACK"));
       
                 var gb = new GrammarBuilder { Culture = ri.Culture };
                 gb.Append(directions);
                
                 var g = new Grammar(gb);
                 speechEngine.LoadGrammar(g);
               

              /*  // Create a grammar from grammar definition XML file.
                using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Grammar1.xml here)))
                {
                    var g = new Grammar(memoryStream);
                    speechEngine.LoadGrammar(g);
                }
            */
                speechEngine.SpeechRecognized += SpeechRecognized;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                speechEngine.SetInputToAudioStream(
                    sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        


     private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e) //handler for speech reconginition
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.4;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "RED":
                        head.Fill = new SolidColorBrush(Colors.Red);
                        break;

                    case "BLUE":
                        head.Fill = new SolidColorBrush(Colors.Blue);
                        break;

                    case "GREEN":
                        head.Fill = new SolidColorBrush(Colors.Green);
                        break;

                    case "BLACK":
                        head.Fill = new SolidColorBrush(Colors.Black);
                        break;
                }
            }
        }


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




        }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

 }
