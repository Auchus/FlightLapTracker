using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FlightLapTracker.Helpers
{
    public static class HtmlExporter
    {
        private const string FilePath = "Results/laps.html";

        public static void ExportToHtml(IEnumerable<Pilot> pilots)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            var html = new StringBuilder();

            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <title>Результаты полёта</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 40px; }");
            html.AppendLine("        table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: center; }");
            html.AppendLine("        th { background-color: #f2f2f2; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <h2>Результаты полёта</h2>");

            html.AppendLine("    <table>");

            // Заголовки столбцов
            html.Append("        <tr><th>ФИО</th><th>Никнейм</th><th>Канал</th>");
            int maxLaps = pilots.Max(p => p.Laps.Count);
            for (int i = 1; i <= maxLaps; i++)
            {
                html.Append($"<th>Круг {i}</th>");
            }
            html.AppendLine("</tr>");

            // Данные по каждому пилоту
            foreach (var pilot in pilots)
            {
                html.Append($"<tr><td>{pilot.FullName}</td><td>{pilot.Nickname}</td><td>{pilot.Channel}</td>");
                foreach (var lap in pilot.Laps)
                {
                    html.Append($"<td>{lap.ToString(@"hh\:mm\:ss\.fff")}</td>");
                }

                // Если у этого пилота меньше кругов, чем у других — заполняем пустыми ячейками
                for (int i = pilot.Laps.Count; i < maxLaps; i++)
                {
                    html.Append("<td></td>");
                }

                html.AppendLine("</tr>");
            }

            html.AppendLine("    </table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            File.WriteAllText(FilePath, html.ToString(), Encoding.UTF8);
        }
    }
}