

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Net.Sockets;
    using System.Net;
    using System.Threading;
    using System.Collections.Generic;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using Microsoft.Speech.Synthesis;
    using Microsoft.VisualBasic;
    using Microsoft.Win32; 

    /// <summary>
    /// @Author: Akhil Acharya 
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 5;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        private SpeechRecognitionEngine speechEngine;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>

        private int frameCount = 0; 
        private int maxFrames = 30;

      
        public MainWindow()
        {
            InitializeComponent();
            ComboBoxFishType.Items.Add("Shark");
            ComboBoxFishType.Items.Add("Clownfish");
            ComboBoxFishType.SelectedItem = "Shark";

            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName;
                string safeFilePath = ofd.SafeFileName;
            }


           
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo GetKinectRecognizer()
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


        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }
           
            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
            if (null == this.sensor)
            {
                this.statusBarText.Text = AirSwimmers.SVSM.Kinect.Properties.Resources.NoKinectReady;
            }

            //SPEECH RECOGNIZER STUFF
            RecognizerInfo ri = GetKinectRecognizer();
            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                var directions = new Choices();
               

                /***FISH DIRECTIONS***/
                directions.Add(new SemanticResultValue("Bruce Turn Left", Constants.FishTypes.Shark + Constants.Directions.Left));
                directions.Add(new SemanticResultValue("Bruce Turn Right", Constants.FishTypes.Shark + Constants.Directions.Right));
                directions.Add(new SemanticResultValue("Bruce Pitch Up", Constants.FishTypes.Shark + Constants.Directions.Up));
                directions.Add(new SemanticResultValue("Bruce Pitch Down", Constants.FishTypes.Shark + Constants.Directions.Down));
                directions.Add(new SemanticResultValue("Bruce Go Forward", Constants.FishTypes.Shark + "F"));
             

                directions.Add(new SemanticResultValue("Nemo Turn Left", Constants.FishTypes.Clownfish + Constants.Directions.Left)); 
                directions.Add(new SemanticResultValue("Nemo Turn Right", Constants.FishTypes.Clownfish + Constants.Directions.Right)); ;
                directions.Add(new SemanticResultValue("Nemo Pitch Up", Constants.FishTypes.Clownfish + Constants.Directions.Up));
                directions.Add(new SemanticResultValue("Nemo Pitch Down", Constants.FishTypes.Clownfish + Constants.Directions.Down));
                directions.Add(new SemanticResultValue("Nemo Go Forward", Constants.FishTypes.Clownfish + "F"));


                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);

         
                var g = new Grammar(gb);
                speechEngine.LoadGrammar(g);

                speechEngine.SpeechRecognized += SpeechRecognized;

                speechEngine.SetInputToAudioStream(
                     sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            //END SPEECH RECOGNIZER STUFF


        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;
            AirSwimmerControl control; 
            
            String baseString = "Recognized: ";   

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
               switch (e.Result.Semantics.Value.ToString())
                {
                   case "SU": 
                       control = new AirSwimmerControl(Constants.FishTypes.Shark); 
                       DebugDisplay.Text = baseString + "UP"; 
                       control.turnUp(); 
                       break; 
                   case "SD":
                       control = new AirSwimmerControl(Constants.FishTypes.Shark); 
                        DebugDisplay.Text = baseString + "DOWN"; 
                       control.turnDown(); 
                       break; 
                   case "SL": 
                       control = new AirSwimmerControl(Constants.FishTypes.Shark); 
                        DebugDisplay.Text = baseString + "LEFT"; 
                       control.turnLeft(); 
                       break; 
                   case "SR": 
                       control = new AirSwimmerControl(Constants.FishTypes.Shark); 
                        DebugDisplay.Text = baseString + "RIGHT"; 
                       control.turnRight(); 
                       break; 
                   case "SF":
                        control = new AirSwimmerControl(Constants.FishTypes.Shark); 
                        DebugDisplay.Text = baseString + "FORWARD";
                        
                       break;
                   case "CF":
                        control = new AirSwimmerControl(Constants.FishTypes.Clownfish); 
                        DebugDisplay.Text = baseString + "FORWARD";
                       
                       break; 
                   case "CU": 
                       control = new AirSwimmerControl(Constants.FishTypes.Clownfish); 
                        DebugDisplay.Text = baseString + "UP"; 
                       control.turnUp(); 
                       break; 
                   case "CL": 
                       control = new AirSwimmerControl(Constants.FishTypes.Clownfish);  
                        DebugDisplay.Text = baseString + "LEFT"; 
                       control.turnLeft(); 
                       break;
                   case "CR": 
                      control = new AirSwimmerControl(Constants.FishTypes.Clownfish); 
                        DebugDisplay.Text = baseString + "RIGHT"; 
                       control.turnRight(); 
                       break; 
                   case "CD": 
                       control = new AirSwimmerControl(Constants.FishTypes.Clownfish); 
                        DebugDisplay.Text = baseString + "DOWN"; 
                       control.turnDown(); 
                       break; 
                }
                 control = null; 
            }
        }


        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
           
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
                                   
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                            getJointData(skel); 
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }


        /// <summary>
        /// Gets joint data and interfaces with control system.
        /// </summary>
        /// <param name="skeleton">Skeleton, gotten each time whenever Skeleton frame is ready</param>
        private void getJointData(Skeleton skeleton)
        {
            //IR Control object
            AirSwimmerControl control;                                          
            
            //Determine if the system should control the Shark or Clownfish
            if(ComboBoxFishType.SelectedValue.Equals("Shark"))                 
            {
                control = new AirSwimmerControl(Constants.FishTypes.Shark); 
            }else
            {
                control = new AirSwimmerControl(Constants.FishTypes.Clownfish); 
            }
            
            //Increment global framecount variable each time the function is called
            frameCount++;
            
            //Create boolean check if the increment goes into the maxFrames variable evenly, allowing the function to continue. 
            Boolean doSend = false;      

            //Set default shoulder values. Used to check if the joint data has already been collected. 
            float shoulderX = -50f;
            float shoulderY = -50f; 

            //Iterate through all joints in the skeleton
            foreach (Joint joint in skeleton.Joints)
            {
                //Get Joint type. If it equals the Right Shoulder, it saves it in the ShoulderX variable
              if (joint.JointType.Equals(JointType.ShoulderRight))
                {
                    shoulderX = joint.Position.X;
                    shoulderY = joint.Position.Y; 
                }
                else if (joint.JointType.Equals(JointType.HandRight))
                {
                    //After the Right Hand joint is found, make certain that that the Shoulder value has been instantiated. 
                    if (shoulderX != -50f && shoulderY != -50f)
                    {
                        //get relative positions from absolute positions of the shoulder and hand joint. 
                        float handX = getRelativeX(joint.Position.X, shoulderX);
                        float handY = getRelativeY(joint.Position.Y, shoulderY);

                        var debugHandData = "Type: " + joint.JointType.ToString() + " X: " + joint.Position.X + " Y: " + joint.Position.Y;
                        var horizontalDirection = " ";
                        var verticalDirection = " ";

                        //Make sure that the global framecount variable goes into the maxFrames value evenly. 
                        //This ensures that a reading is only taken once every 30 frames, or once every second. 
                        if (frameCount % maxFrames == 0)
                        {
                            doSend = true;
                        }
                      
                        //Detect Vertical Direction
                        if (handY > .2)
                        {
                            if (doSend)
                            {
                                DebugListBox.Items.Add("Send UP called..");
                                control.turnUp(); 
                            }
                            verticalDirection = "UP";
                        }
                        else if (handY < -.2)
                        {
                            if (doSend)
                            {
                                DebugListBox.Items.Add("Send DOWN called..");
                                control.turnDown(); 
                            }
                            verticalDirection = "DOWN";
                        }
                        //Do nothing for "null space"
                        else if(handY > -.2 && handY < .2)
                        {                          
                            verticalDirection = "LAST";
                        }

                        //Detect Horizontal Direction
                        if (handX > .2)
                        {
                            if (doSend)
                            {
                                DebugListBox.Items.Add("Send RIGHT called..");
                                control.turnRight(); 
                            }
                            horizontalDirection = "RIGHT";
                        }
                        else if (handX < -.2)
                        {
                            if (doSend)
                            {
                                DebugListBox.Items.Add("Send LEFT called..");
                                control.turnLeft(); 
                            }
                            horizontalDirection = "LEFT";
                        }
                        //Do nothing for null space
                        else if(handX > -.2 && handX < .2)
                        {
                            horizontalDirection = "FORWARD";
                        }
                        String debugData = "Fish: " + ComboBoxFishType.SelectedValue + " Vertical Direction " + verticalDirection + " Horizontal Direction: " + horizontalDirection + " " + debugHandData;
                        DebugDisplay.Text = debugData;
                    }
                }
            }
            //Free memory be de-instantiating control object. Should help with garbage collection
            control = null; 
        } 

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">Skeleton to draw</param>
        /// <param name="drawingContext">Drawing context to Draw on</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            frameCount++; 
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);
            
            
            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            /*
            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
            */


            // Render Joints        
            foreach (Joint joint in skeleton.Joints)
            {

                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush; 
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    if (joint.JointType.Equals(JointType.HandRight))
                    {
                        drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness * 2, JointThickness * 3);
                    }
                    else if (joint.JointType.Equals(JointType.HandLeft))
                    {
                        drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                    }
                }
            }
            
        }

        /// <summary>
        /// Delete this later. Implemented better already in AirSwimmerControl class
        /// </summary>
        /// <param name="fishtype"></param>
        /// <param name="direction"></param>
        private void SendMoveSignal(String fishtype, String direction)
        {
            String repeat = "0"; 
            String arguments = "AirSwimmer " + fishtype + direction + " " + repeat;

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Constants.FileLocations.lircLocation;
            startInfo.Arguments = arguments;
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                DebugListBox.Items.Add("Exception: " + e.ToString());
            }
        }

        /// <summary>
        /// Get relative X value: As compared to the shoulder position. 
        /// </summary>
        /// <param name="handX">X position of hand</param>
        /// <param name="shoulderX">X position of shoulder</param>
        /// <returns></returns>
        private float getRelativeX(float handX, float shoulderX)
        {
            return handX - shoulderX; 
        }

        /// <summary>
        /// Get relative Y value: As compared to the shoulder position. 
        /// </summary>
        /// <param name="handY">Y position of hand</param>
        /// <param name="shoulderY">Y position of shoulder</param>
        /// <returns></returns>
        private float getRelativeY(float handY, float shoulderY)
        {
            return handY - shoulderY; 
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

    }
}