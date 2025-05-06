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

        public static void ExportToHtml(List<Pilot> pilots)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var html = BuildHtml(pilots);
                File.WriteAllText(FilePath, html, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка сохранения HTML: {ex.Message}");
            }
        }

        private static string BuildHtml(List<Pilot> pilots)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='ru'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <title>Результаты полётов</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 0; padding: 80px 20px 20px; color: white; position: relative; }");
            html.AppendLine("        body::before {");
            html.AppendLine("            content: '';");
            html.AppendLine("            position: fixed;");
            html.AppendLine("            top: 0; left: 0; right: 0; bottom: 0;");
            html.AppendLine("            background: url('C://Users//alexr//Source//Repos//FlightLapTracker//FlightLapTracker//Sounds//Заставка2.jpg') center/cover no-repeat;");
            html.AppendLine("            filter: blur(5px);");
            html.AppendLine("            z-index: -1;");
            html.AppendLine("        }");
            html.AppendLine("        .tab { overflow: hidden; background: #333; position: fixed; top: 0; width: 100%; z-index: 1000; }");
            html.AppendLine("        .tab button { background: inherit; float: left; border: none; outline: none; cursor: pointer; padding: 14px 16px; color: white; }");
            html.AppendLine("        .tab button:hover { background: #555; }");
            html.AppendLine("        .tab button.active { background: #777; }");
            html.AppendLine("        .tabcontent { display: none; margin-top: 20px; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; background: rgba(0,0,0,0.7); }");
            html.AppendLine("        th, td { padding: 10px; text-align: center; border: 1px solid #444; }");
            html.AppendLine("        th { background: #555; }");
            html.AppendLine("        .best { background: yellow; color: black; font-weight: bold; }");
            html.AppendLine("        h2 { color: white; margin-top: 40px; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Вкладки
            html.AppendLine("<div class='tab'>");
            html.AppendLine("    <button class='tablinks active' onclick=\"openTab(event, 'Participants')\">Участники</button>");
            html.AppendLine("    <button class='tablinks' onclick=\"openTab(event, 'Qualification')\">Квалификация</button>");
            html.AppendLine("    <button class='tablinks' onclick=\"openTab(event, 'Race')\">Гонка</button>");
            html.AppendLine("    <button class='tablinks' onclick=\"openTab(event, 'CurrentFlight')\">Текущий полёт</button>");
            html.AppendLine("</div>");

            // Участники
            html.AppendLine("<div id='Participants' class='tabcontent' style='display:block'>");
            html.AppendLine("    <h2>Участники</h2>");
            var participants = ExcelExporter.LoadParticipantsFromSheet();
            html.Append(BuildParticipantTable(participants));
            html.AppendLine("</div>");

            // Квалификация
            html.AppendLine("<div id='Qualification' class='tabcontent'>");
            html.AppendLine("    <h2>Квалификация</h2>");
            html.Append(BuildTable(pilots.Where(p => p.Laps.Count > 0 && p.Laps.Count <= 4).OrderBy(p => p.Laps.Min(l => l.Duration)), false));
            html.AppendLine("</div>");

            // Гонка
            html.AppendLine("<div id='Race' class='tabcontent'>");
            html.AppendLine("    <h2>Гонка</h2>");
            html.Append(BuildTable(pilots.Where(p => p.Laps.Count > 0 && p.Laps.Count <= 2).OrderBy(p => p.Laps.Min(l => l.Duration)), true));
            html.AppendLine("</div>");

            // Текущий полёт
            html.AppendLine("<div id='CurrentFlight' class='tabcontent'>");
            html.AppendLine("    <h2>Текущий полёт</h2>");
            var currentPilots = ExcelExporter.LoadPilotsFromSheet("Текущий полёт");
            html.Append(BuildTable(currentPilots.Where(p => p.Laps.Count > 0), false));
            html.AppendLine("</div>");

            // Скрипт переключения вкладок
            html.AppendLine("<script>");
            html.AppendLine("function openTab(evt, tabName) {");
            html.AppendLine("    var i, tabcontent, tablinks;");
            html.AppendLine("    tabcontent = document.getElementsByClassName('tabcontent');");
            html.AppendLine("    for (i = 0; i < tabcontent.length; i++) tabcontent[i].style.display = 'none';");
            html.AppendLine("    tablinks = document.getElementsByClassName('tablinks');");
            html.AppendLine("    for (i = 0; i < tablinks.length; i++) tablinks[i].className = tablinks[i].className.replace(' active', '');");
            html.AppendLine("    document.getElementById(tabName).style.display = 'block';");
            html.AppendLine("    evt.currentTarget.className += ' active';");
            html.AppendLine("}");
            html.AppendLine("</script>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private static string BuildTable(IEnumerable<Pilot> pilots, bool isRaceMode)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ФИО</th><th>Никнейм</th><th>Канал</th>");

            int maxLaps = isRaceMode ? 2 : 4;
            for (int i = 1; i <= maxLaps; i++)
                sb.Append($"<th>Круг {i}</th>");

            sb.AppendLine("<th>Лучшее время</th></tr>");

            foreach (var pilot in pilots)
            {
                sb.Append($"<tr><td>{pilot.FullName}</td><td>{pilot.Nickname}</td><td>{pilot.Channel}</td>");
                for (int i = 0; i < maxLaps; i++)
                    sb.Append($"<td>{(i < pilot.Laps.Count ? pilot.Laps[i].Duration.ToString(@"hh\:mm\:ss\.fff") : "")}</td>");

                var bestLap = pilot.Laps.Count > 0 ? pilot.Laps.Min(l => l.Duration) : TimeSpan.Zero;
                sb.AppendLine(bestLap != TimeSpan.Zero
                    ? $"<td class='best'>{bestLap:hh\\:mm\\:ss\\.fff}</td></tr>"
                    : "<td></td></tr>");
            }

            sb.AppendLine("</table>");
            return sb.ToString();
        }

        private static string BuildParticipantTable(IEnumerable<Pilot> participants)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ФИО</th><th>Никнейм</th><th>Частота</th></tr>");
            foreach (var p in participants)
                sb.AppendLine($"<tr><td>{p.FullName}</td><td>{p.Nickname}</td><td>{p.Channel}</td></tr>");
            sb.AppendLine("</table>");
            return sb.ToString();
        }
    }
}