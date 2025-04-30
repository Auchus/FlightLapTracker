using System.Collections.Generic;
using System;

public class Pilot
{
    public string FullName { get; set; }
    public string Nickname { get; set; }
    public string Channel { get; set; }
    public long TelegramId { get; set; }
    public List<TimeSpan> Laps { get; } = new();

    public int ExportedLapsCount { get; set; } = 0; // Сколько лап уже записано в Excel

}