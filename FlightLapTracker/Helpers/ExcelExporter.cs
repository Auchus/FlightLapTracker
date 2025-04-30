using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlightLapTracker.Helpers
{
    public static class ExcelExporter
    {
        private const string FilePath = "Results/laps.xlsx";
        private const string SheetName = "Лапы";

        /// <summary>
        /// Экспортирует только новые лапы (те, которые ещё не были записаны)
        /// </summary>
        public static void ExportNewLaps(IEnumerable<Pilot> pilots)
        {
            foreach (var pilot in pilots)
            {
                var newLaps = pilot.Laps.Skip(pilot.ExportedLapsCount).ToList();

                if (!newLaps.Any())
                    continue;

                ExportLapsToFile(newLaps, pilot);
                pilot.ExportedLapsCount += newLaps.Count;
            }
        }

        /// <summary>
        /// Записывает указанные лапы в Excel-файл
        /// </summary>
        private static void ExportLapsToFile(List<TimeSpan> laps, Pilot pilot)
        {
            bool fileExists = File.Exists(FilePath);

            using var workbook = fileExists ? new XLWorkbook(FilePath) : new XLWorkbook();

            IXLWorksheet worksheet;

            // Проверяем, есть ли нужный лист
            worksheet = workbook.Worksheets.FirstOrDefault(ws =>
                ws.Name.Trim().Equals(SheetName, StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.Add(SheetName);

            // Добавляем заголовки, если лист пустой
            if (worksheet.LastRowUsed() == null)
            {
                worksheet.Cell(1, 1).Value = "Пилот";
                worksheet.Cell(1, 2).Value = "Никнейм";
                worksheet.Cell(1, 3).Value = "Канал";
                worksheet.Cell(1, 4).Value = "Дата";
                worksheet.Cell(1, 5).Value = "Время лапа";

                worksheet.Row(1).Style.Font.Bold = true;
                worksheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Начинаем запись данных после последней строки
            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            int row = lastRow + 1;

            foreach (var lap in laps)
            {
                worksheet.Cell(row, 1).Value = pilot.FullName;
                worksheet.Cell(row, 2).Value = pilot.Nickname;
                worksheet.Cell(row, 3).Value = pilot.Channel;
                worksheet.Cell(row, 4).Value = DateTime.Now.ToString("dd.MM.yyyy");
                worksheet.Cell(row, 5).Value = lap.ToString(@"hh\:mm\:ss\.fff");
                worksheet.Cell(row, 5).Style.NumberFormat.Format = @"hh\:mm\:ss\.fff";


                row++;
            }

            // Сохраняем изменения
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            workbook.SaveAs(FilePath);
        }
    }
}