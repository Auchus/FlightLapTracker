using System.Collections.Generic;
using FlightLapTracker.Models;

namespace FlightLapTracker.Models
{
    public static class PilotExtensions
    {
        public static Pilot Clone(this Pilot pilot)
        {
            return new Pilot
            {
                FullName = pilot.FullName,
                Nickname = pilot.Nickname,
                Channel = pilot.Channel,
                TelegramId = pilot.TelegramId,

                // Копируем только лапы
                Laps = pilot.Laps.ConvertAll(l => new Lap(l.TotalTime, null)),

                ExportedLapsCount = pilot.ExportedLapsCount
            };
        }
    }
}