using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Constants
{
    /// <summary>
    /// @Author: Akhil Acharya 
    /// </summary>

    public static class FishTypes
    {
        public static String Clownfish = "C";
        public static String Shark = "S"; 
    }
    /// <summary>
    /// Constants for important direction strings. 
    /// </summary>
    public static class Directions
    {
        public static String Left = "L";
        public static String Right = "R";
        public static String Up = "U"; 
        public static String Down = "D"; 
    }
    /// <summary>
    /// Constants for important delays/timing assistants. 
    /// </summary>
    public static class Delays
    {
        public static long forwardDelay = 500;
        public static long generalDirectionDelay = 1500; 
    }
    /// <summary>
    /// Constants for important file locations. 
    /// </summary>
    public static class FileLocations
    {
        public static String lircLocation =  "E:\\Air Swimmers SVSM Kinect\\IRToy\\WinLirc-9f\\Transmit.exe";
        public static String killLocation = "E:\\Air Swimmers SVSM Kinect\\IRToy\\winlirc-0.9.0e\\WinLIRC\\killall.bat"; 
    }
}
