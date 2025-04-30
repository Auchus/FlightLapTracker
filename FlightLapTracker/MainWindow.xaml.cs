using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Threading;
using DocumentFormat.OpenXml.Drawing;
using FlightLapTracker.Helpers;
using FlightLapTracker.Services;

namespace FlightLapTracker
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer countDownTimer;
        private DispatcherTimer flightTimer;
        private int currentCount = 3;
        private TimeSpan flightTime = TimeSpan.FromSeconds(180);
        private List<TimeSpan> lapsPilot1 = new List<TimeSpan>();
        private List<TimeSpan> lapsPilot2 = new List<TimeSpan>();
        private Stopwatch flightStopwatch = new Stopwatch();
        private DateTime? startTime; // Чтобы хранить начало полёта


        private SoundPlayer startPlayer = new SoundPlayer("C:\\Users\\admin\\source\\repos\\FlightLapTracker\\FlightLapTracker\\Sounds\\start.wav");
        private SoundPlayer warningPlayer = new SoundPlayer("C:\\Users\\admin\\source\\repos\\FlightLapTracker\\FlightLapTracker\\Sounds\\warning.wav");
        private bool hasPlayedWarning = false;

        private readonly TelegramBotService botService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimers();
            botService = new TelegramBotService();
            botService.PilotRegistered += OnPilotRegistered;



        }

        private void InitializeTimers()
        {
            countDownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            countDownTimer.Tick += CountDownTimer_Tick;

            flightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.1) }; // 10 раз в секунду
            flightTimer.Tick += FlightTimer_Tick;
        }

        private void StartTestFlight_Click(object sender, RoutedEventArgs e)
        {
            StartCountdown();
        }

        private void StartCountdown()
        {
            currentCount = 3;
            CountdownText.Text = currentCount.ToString();
            StatusText.Text = "Обратный отсчёт...";

            // Проигрываем общий звук (3-2-1 + старт)
            startPlayer.Play();

            // Создаём таймер для обновления текста на экране
            var updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            updateTimer.Tick += (s, e) =>
            {
                currentCount--;

                if (currentCount > 0)
                {
                    CountdownText.Text = currentCount.ToString();
                }
                else
                {
                    updateTimer.Stop();
                    StartFlight();
                }
            };

            updateTimer.Start();
        }

        private void CountDownTimer_Tick(object sender, EventArgs e)
        {
            if (currentCount > 0)
            {
                startPlayer.Play();
                currentCount--;
                CountdownText.Text = currentCount.ToString();
            }
            else
            {
                countDownTimer.Stop();
                startPlayer.Play();
                StartFlight();
            }
        }

        private void StartFlight()
        {
            flightStopwatch.Restart();
            startTime = DateTime.Now;
            hasPlayedWarning = false; // Сбрасываем флаг

            StatusText.Text = "Полёт начат!";
            CountdownText.Text = "Полёт...";
            flightTimer.Start();
        }

        private void FlightTimer_Tick(object sender, EventArgs e)
        {
            var elapsed = flightStopwatch.Elapsed;
            var remaining = flightTime - elapsed;

            if (remaining.TotalSeconds <= 10 && !hasPlayedWarning)
            {
                warningPlayer.Play();
                hasPlayedWarning = true; // Защита от повторного проигрывания
            }

            if (elapsed >= flightTime)
            {
                flightTimer.Stop();
                flightStopwatch.Stop();
                StatusText.Text = "Полёт окончен.";
            }

            HtmlExporter.ExportToHtml(botService.RegisteredPilots);
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Q:
                    RecordLap(1);
                    break;
                case System.Windows.Input.Key.P:
                    RecordLap(2);
                    break;
            }
            base.OnKeyDown(e);
        }

        private void RecordLap(int pilotNumber)
        {
            if (!startTime.HasValue)
                return;

            var lapTime = flightStopwatch.Elapsed; // Используем точное время от начала полёта

            if (pilotNumber == 1 && botService.RegisteredPilots.Count >= 1)
            {
                var pilot = botService.RegisteredPilots[0];
                pilot.Laps.Add(lapTime);
                LapsListPilot1.Items.Add(lapTime.ToString(@"hh\:mm\:ss\.fff"));

                ExcelExporter.ExportNewLaps(new[] { pilot });
            }
            else if (pilotNumber == 2 && botService.RegisteredPilots.Count >= 2)
            {
                var pilot = botService.RegisteredPilots[1];
                pilot.Laps.Add(lapTime);
                LapsListPilot2.Items.Add(lapTime.ToString(@"hh\:mm\:ss\.fff"));

                ExcelExporter.ExportNewLaps(new[] { pilot });
            }
        }
        private void OnPilotRegistered(Pilot pilot)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Зарегистрирован пилот:\n{pilot.FullName} ({pilot.Channel})");
            });
        }
    }
}