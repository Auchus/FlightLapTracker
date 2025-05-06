using System;

public class Lap
{
    public TimeSpan TotalTime { get; }
    public TimeSpan Duration { get; }

    public Lap(TimeSpan totalTime, Lap previousLap = null)
    {
        TotalTime = totalTime;
        if (previousLap != null)
        {
            Duration = totalTime - previousLap.TotalTime;
        }
        else
        {
            Duration = totalTime;
        }
    }
}