using ClosedXML.Excel;
using FlightLapTracker.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlightLapTracker.Helpers
{
    public static class ExcelExporter
    {
        private const string FilePath = "Results/laps.xlsx";

        public static void ClearSheet(string sheetName)
        {
            if (!File.Exists(FilePath)) return;

            using var workbook = new XLWorkbook(FilePath);
            if (workbook.Worksheets.TryGetWorksheet(sheetName, out var worksheet))
            {
                foreach (var row in worksheet.RowsUsed().Skip(1)) row.Delete();
            }
            workbook.Save();
        }


        public static void ExportPilots(IEnumerable<Pilot> pilots, bool isRaceMode, string sheetName = null)
        {
            using var workbook = File.Exists(FilePath) ? new XLWorkbook(FilePath) : new XLWorkbook();

            if (sheetName == null)
            {
                string mainSheetName = isRaceMode ? "Гонка" : "Лапы";
                IXLWorksheet mainSheet = GetOrCreateWorksheet(workbook, mainSheetName);

                WriteHeaders(mainSheet, isRaceMode);

                int lastRow = mainSheet.RowsUsed().Count() + 1;

                foreach (var pilot in pilots)
                {
                    WritePilotData(mainSheet, lastRow++, pilot, isRaceMode);
                }
            }
            else
            {
                IXLWorksheet tempSheet = GetOrCreateWorksheet(workbook, sheetName);

                // ❌ Убираем удаление всех строк, кроме первой:
                foreach (var row in tempSheet.RowsUsed().Skip(1)) row.Delete(); // ✅ Теперь удаляем только данные, не шапку

                WriteHeaders(tempSheet, isRaceMode); // ✅ Переписываем заголовки (можно убрать, если они уже есть)

                int lastRow = 2; // ✅ Начинаем с 2 строки

                foreach (var pilot in pilots)
                {
                    WritePilotData(tempSheet, lastRow++, pilot, isRaceMode);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            workbook.SaveAs(FilePath);
        }

        public static List<Pilot> LoadPilotsFromSheet()
        {
            if (!File.Exists(FilePath)) return new List<Pilot>();
            using var workbook = new XLWorkbook(FilePath);
            var pilots = new List<Pilot>();

            foreach (var sheetName in new[] { "Лапы", "Гонка" })
            {
                if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var worksheet)) continue;
                bool isRace = sheetName == "Гонка";
                int maxLaps = isRace ? 2 : 4;
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var fullName = row.Cell(1).GetString();
                    if (string.IsNullOrWhiteSpace(fullName)) continue;
                    var pilot = new Pilot
                    {
                        FullName = fullName,
                        Nickname = row.Cell(2).GetString(),
                        Channel = row.Cell(3).GetString()
                    };
                    if (long.TryParse(row.Cell(10).GetString(), out var id))
                        pilot.TelegramId = id;
                    TimeSpan totalTime = TimeSpan.Zero;
                    for (int i = 0; i < maxLaps; i++)
                    {
                        if (TimeSpan.TryParse(row.Cell(5 + i).GetString(), out var duration))
                        {
                            totalTime += duration;
                            pilot.Laps.Add(new Lap(totalTime, pilot.Laps.LastOrDefault()));
                        }
                    }
                    pilots.Add(pilot);
                }
            }
            return pilots;
        }

        public static List<Pilot> LoadPilotsFromSheet(string sheetName)
        {
            if (!File.Exists(FilePath)) return new List<Pilot>();
            using var workbook = new XLWorkbook(FilePath);
            if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var worksheet)) return new List<Pilot>();

            var pilots = new List<Pilot>();
            bool isRace = sheetName == "Гонка";
            int maxLaps = isRace ? 2 : 4;

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var fullName = row.Cell(1).GetString();
                if (string.IsNullOrWhiteSpace(fullName)) continue;
                var pilot = new Pilot
                {
                    FullName = fullName,
                    Nickname = row.Cell(2).GetString(),
                    Channel = row.Cell(3).GetString()
                };
                if (long.TryParse(row.Cell(10).GetString(), out var id))
                    pilot.TelegramId = id;
                TimeSpan totalTime = TimeSpan.Zero;
                for (int i = 0; i < maxLaps; i++)
                {
                    if (TimeSpan.TryParse(row.Cell(5 + i).GetString(), out var duration))
                    {
                        totalTime += duration;
                        pilot.Laps.Add(new Lap(totalTime, pilot.Laps.LastOrDefault()));
                    }
                }
                pilots.Add(pilot);
            }
            return pilots;
        }

        public static List<Pilot> LoadParticipantsFromSheet()
        {
            const string sheetName = "Участники";
            if (!File.Exists(FilePath)) return new List<Pilot>();
            using var workbook = new XLWorkbook(FilePath);
            if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var worksheet)) return new List<Pilot>();

            var participants = new List<Pilot>();
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var fullName = row.Cell(1).GetString();
                if (string.IsNullOrWhiteSpace(fullName)) continue;
                var pilot = new Pilot
                {
                    FullName = fullName,
                    Nickname = row.Cell(2).GetString(),
                    Channel = row.Cell(3).GetString()
                };
                participants.Add(pilot);
            }
            return participants;
        }

        private static void WritePilotData(IXLWorksheet sheet, int row, Pilot pilot, bool isRaceMode)
        {
            sheet.Cell(row, 1).Value = pilot.FullName;
            sheet.Cell(row, 2).Value = pilot.Nickname;
            sheet.Cell(row, 3).Value = pilot.Channel;
            sheet.Cell(row, 4).Value = DateTime.Now.ToString("dd.MM.yyyy");
            int maxLaps = isRaceMode ? 2 : 4;
            for (int i = 0; i < pilot.Laps.Count && i < maxLaps; i++)
            {
                sheet.Cell(row, 5 + i).Value = pilot.Laps[i].Duration.ToString(@"hh\:mm\:ss\.fff");
                sheet.Cell(row, 5 + i).Style.NumberFormat.Format = @"hh\:mm\:ss\.fff";
            }
            var bestLap = pilot.Laps.Count > 0 ? pilot.Laps.Min(l => l.Duration) : TimeSpan.Zero;
            sheet.Cell(row, 9).Value = bestLap != TimeSpan.Zero ? bestLap.ToString(@"hh\:mm\:ss\.fff") : "";
            sheet.Cell(row, 10).Value = pilot.TelegramId.ToString();
        }

        private static void WriteHeaders(IXLWorksheet worksheet, bool isRaceMode)
        {
            string[] headers = { "Пилот", "Никнейм", "Канал", "Дата" };
            for (int i = 0; i < headers.Length; i++)
                worksheet.Cell(1, i + 1).Value = headers[i];

            int maxLaps = isRaceMode ? 2 : 4;
            for (int i = 1; i <= maxLaps; i++)
                worksheet.Cell(1, 4 + i).Value = $"Круг {i}";

            worksheet.Cell(1, 9).Value = "Лучшее время";
            worksheet.Cell(1, 10).Value = "TelegramId";
            worksheet.Row(1).Style.Font.Bold = true;
        }

        private static IXLWorksheet GetOrCreateWorksheet(XLWorkbook workbook, string sheetName)
        {
            return workbook.Worksheets.FirstOrDefault(ws => ws.Name.Equals(sheetName)) ?? workbook.Worksheets.Add(sheetName);
        }
    }
}