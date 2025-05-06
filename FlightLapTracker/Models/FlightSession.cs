// FlightSession.cs
using System;
using System.Collections.Generic;

namespace FlightLapTracker.Models
{
    public class FlightSession
    {
        public DateTime StartTime { get; set; }
        public List<Pilot> Pilots { get; } = new();
    }
}