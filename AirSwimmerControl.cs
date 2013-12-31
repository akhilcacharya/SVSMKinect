using System;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Net; 

public class AirSwimmerControl
{
    static String fishType;
    
  /// <summary>
  /// Controller Constructor
  /// </summary>
  /// <param name="fish">Fish that is to be controlled - data in Constants.FishType class</param>
	public AirSwimmerControl(String fish)
	{
       fishType = fish;
	}

    public String turnLeft()
    {
       return SendMoveSignal(Constants.Directions.Left); 
    }

    public String turnRight()
    {
       return SendMoveSignal(Constants.Directions.Right); 
    }

    public String turnUp()
    {
       return SendMoveSignal(Constants.Directions.Up); 
    }

    public String turnDown()
    {
      return SendMoveSignal(Constants.Directions.Down); 
    }

    public String  goStraight()
    {
        SendMoveSignal(Constants.Directions.Left);
        SendMoveSignal(Constants.Directions.Right);
        SendMoveSignal(Constants.Directions.Right); 
        SendMoveSignal(Constants.Directions.Left);
        return "Moved!"; 
    }

    private string SendMoveSignal(String direction)
    {
        String repeat = "0";
        String arguments = "AirSwimmer " + fishType + direction + " " + repeat;
        ProcessStartInfo startInfo = new ProcessStartInfo();
        
        startInfo.FileName = Constants.FileLocations.lircLocation;
       
        startInfo.Arguments = arguments;
        try
        {
            Process.Start(startInfo);
            return "Moved"; 
        }
        catch (Exception e)
        {
          return "Exception: " + e.ToString();
        }
    }

    private String SendMoveSignal(String direction, int repeats)
    {
        String repeat = repeats.ToString(); 
        String arguments = "AirSwimmer " + fishType + direction + " " + repeat;

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = Constants.FileLocations.lircLocation;       
        startInfo.Arguments = arguments;
        try
        {
            Process.Start(startInfo);
            return "Moved!";
        }
        catch (Exception e)
        {
            return "Exception: " + e.ToString();
        }
    }

    public void close()
    {

    }

}

