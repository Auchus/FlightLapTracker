using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FlightLapTracker.Helpers;
using FlightLapTracker.Models;
using FlightLapTracker.Services;

namespace FlightLapTracker
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer flightTimer;
        private DispatcherTimer timeElapsedTimer;
        private int currentCount = 3;
        private TimeSpan flightTime = TimeSpan.FromSeconds(180);
        private Stopwatch flightStopwatch = new Stopwatch();
        private DateTime? startTime;
        private Dictionary<int, DateTime?> lastKeyPressTime = new()
        {
            { 1, null },
            { 2, null }
        };
        private readonly TelegramBotService botService;
        private List<Pilot> allPilotsHistory = new();
        private bool isRaceMode = false;

        private SoundPlayer soundPlayer = new();

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimers();
            LoadAllPilotsHistory();
            botService = new TelegramBotService();
            botService.PilotRegistered += OnPilotRegistered;
        }

        private void LoadAllPilotsHistory()
        {
            allPilotsHistory = ExcelExporter.LoadPilotsFromSheet();
            var pilotsFromJson = PilotDataStore.Load();
            allPilotsHistory.AddRange(pilotsFromJson.Where(p => !allPilotsHistory.Any(ep => ep.TelegramId == p.TelegramId)));
        }

        private void InitializeTimers()
        {
            flightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.1) };
            flightTimer.Tick += FlightTimer_Tick;
            timeElapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timeElapsedTimer.Tick += TimeElapsedTimer_Tick;
        }

        private void StartTestFlight_Click(object sender, RoutedEventArgs e)
        {
            if (botService.RegisteredPilots.Count < 2)
            {
                MessageBox.Show("Нужно зарегистрировать минимум двух пилотов.");
                return;
            }

            // ✅ Начинаем отсчёт
            StartCountdown();

            // ✅ Проигрываем звук в отдельном потоке
            Task.Run(() => PlaySound("C://Users//alexr//Source//Repos//FlightLapTracker//FlightLapTracker//Sounds//start.wav"));
        }

        private void StartCountdown()
        {
            currentCount = 3;
            CountdownText.Text = currentCount.ToString();
            StatusText.Text = "Обратный отсчёт...";

            var updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            EventHandler tickHandler = null;
            tickHandler = (s, args) =>
            {
                currentCount--;
                CountdownText.Text = currentCount.ToString();

                if (currentCount <= 0)
                {
                    updateTimer.Stop();
                    updateTimer.Tick -= tickHandler;
                    StartFlight();
                }
            };

            updateTimer.Tick += tickHandler;
            updateTimer.Start();
        }

        private void PlaySound(string path)
        {
            try
            {
                using var player = new SoundPlayer(path);
                player.PlaySync(); // Проигрываем синхронно, но в отдельном потоке
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка воспроизведения звука: {ex.Message}");
            }
        }

        private void StartFlight()
        {
            flightStopwatch.Restart();
            startTime = DateTime.Now;
            StatusText.Text = "Полёт начат!";
            CountdownText.Text = "Полёт...";
            TimeElapsedText.Text = "Прошло времени: 00:00:00.000";

            // Очищаем лист "Текущий полёт" перед началом
            ExcelExporter.ClearSheet("Текущий полёт");

            var currentPilots = botService.RegisteredPilots.Take(2).ToList();
            ExcelExporter.ExportPilots(currentPilots, isRaceMode, "Текущий полёт");
            HtmlExporter.ExportToHtml(ExcelExporter.LoadPilotsFromSheet());

            flightTimer.Start();
            timeElapsedTimer.Start();
        }

        private void EndFlightEarly_Click(object sender, RoutedEventArgs e)
        {
            EndFlight();
        }

        private void EndFlight()
        {
            flightTimer.Stop();
            flightStopwatch.Stop();
            timeElapsedTimer.Stop();
            StatusText.Text = "Полёт окончен.";

            ExcelExporter.ExportPilots(botService.RegisteredPilots, isRaceMode);

            allPilotsHistory = ExcelExporter.LoadPilotsFromSheet();
            HtmlExporter.ExportToHtml(allPilotsHistory);
        }

        private void TimeElapsedTimer_Tick(object sender, EventArgs e)
        {
            if (flightStopwatch.IsRunning)
            {
                TimeElapsedText.Text = $"Прошло времени: {flightStopwatch.Elapsed:hh\\:mm\\:ss\\.fff}";

                // За 10 секунд до конца
                if (flightStopwatch.Elapsed >= flightTime.Subtract(TimeSpan.FromSeconds(10)))
                {
                    Task.Run(() => PlaySound("C://Users//alexr//Source//Repos//FlightLapTracker//FlightLapTracker//Sounds//waning.wav"));
                }
            }
        }

        private void FlightTimer_Tick(object sender, EventArgs e)
        {
            var elapsed = flightStopwatch.Elapsed;
            if (elapsed >= flightTime ||
                (isRaceMode && botService.RegisteredPilots.Take(2).All(p => p.Laps.Count >= 2)) ||
                (!isRaceMode && botService.RegisteredPilots.Take(2).All(p => p.Laps.Count >= 4)))
            {
                EndFlight();
            }
            else if ((int)elapsed.TotalSeconds % 5 == 0)
            {
                var currentPilots = botService.RegisteredPilots.Take(2).ToList();
                ExcelExporter.ExportPilots(currentPilots, isRaceMode, "Текущий полёт");
                HtmlExporter.ExportToHtml(ExcelExporter.LoadPilotsFromSheet());
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Q: RecordLap(1); break;
                case System.Windows.Input.Key.P: RecordLap(2); break;
            }
            base.OnKeyDown(e);
        }

        private void RecordLap(int pilotNumber)
        {
            if (!startTime.HasValue || botService.RegisteredPilots.Count < pilotNumber) return;
            var pilot = botService.RegisteredPilots[pilotNumber - 1];
            int maxLaps = isRaceMode ? 2 : 4;
            if (pilot.Laps.Count >= maxLaps) return;
            var now = DateTime.Now;
            if (lastKeyPressTime[pilotNumber] is { } last && (now - last).TotalSeconds < 5) return;
            var lapTime = flightStopwatch.Elapsed;
            var newLap = new Lap(lapTime, pilot.Laps.LastOrDefault());
            pilot.Laps.Add(newLap);
            lastKeyPressTime[pilotNumber] = now;

            Dispatcher.Invoke(() =>
            {
                var lapsList = pilotNumber == 1 ? LapsListPilot1 : LapsListPilot2;
                lapsList.Items.Clear();
                pilot.Laps.ForEach(lap => lapsList.Items.Add($"{lap.Duration:hh\\:mm\\:ss\\.fff}"));
            });

            var currentPilots = botService.RegisteredPilots.Take(2).ToList();
            ExcelExporter.ExportPilots(currentPilots, isRaceMode, "Текущий полёт");
            HtmlExporter.ExportToHtml(ExcelExporter.LoadPilotsFromSheet());
        }

        private void OnPilotRegistered(Pilot pilot) => Dispatcher.Invoke(() =>
            MessageBox.Show($"Зарегистрирован пилот:\n{pilot.FullName} ({pilot.Channel})"));

        private void QualificationMode_Click(object sender, RoutedEventArgs e)
        {
            isRaceMode = false;
            StatusText.Text = "Режим: Квалификация (4 круга)";
        }

        private void RaceMode_Click(object sender, RoutedEventArgs e)
        {
            isRaceMode = true;
            StatusText.Text = "Режим: Гонка (2 круга)";
        }
    }
}