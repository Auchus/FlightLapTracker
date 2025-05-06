using FlightLapTracker.Models;
using System.Collections.Generic;

public class Pilot
{
    public string FullName { get; set; }
    public string Nickname { get; set; }
    public string Channel { get; set; }
    public long TelegramId { get; set; }

    // Список кругов (лапов)
    public List<Lap> Laps { get; set; } = new();
    public int ExportedLapsCount { get; set; } = 0;
}