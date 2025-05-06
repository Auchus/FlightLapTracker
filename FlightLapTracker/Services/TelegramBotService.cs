using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FlightLapTracker.Services
{


    public class TelegramBotService
    {

        // Внутри класса TelegramBotService
        private class UserState
        {
            public string FullName { get; set; }
            public bool IsFullNameReceived { get; set; }
        }

        private readonly Dictionary<long, UserState> userStates = new();
        private readonly ITelegramBotClient botClient;
        private readonly List<Pilot> registeredPilots = new List<Pilot>();
        private readonly List<string> availableChannels = new List<string> { "R1", "R2", "R3", "R4", "R5" };

        public List<Pilot> RegisteredPilots => registeredPilots;

        public event Action<Pilot> PilotRegistered;

        public TelegramBotService()
        {
            botClient = new TelegramBotClient("8154339160:AAHeyS4XNjl0n_ynK-nU_4UphIYQNQyjkLQ");

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            // Запуск бота
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions,
                cts.Token
            );

            Console.WriteLine("Telegram бот запущен.");
        }

        // Обработка сообщений
        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var message = update.Message;
            var chatId = message.Chat.Id;
            var userId = message.From.Id;

            if (message.Text.ToLower() == "/start")
            {
                // Начало регистрации
                userStates[userId] = new UserState();
                await client.SendMessage(chatId, "Привет! Как тебя зовут? (ФИО)", cancellationToken: ct);
            }
            else if (!userStates.ContainsKey(userId))
            {
                // Пользователь начал писать без /start
                await client.SendMessage(chatId, "Напиши /start для начала регистрации.", cancellationToken: ct);
            }
            else
            {
                var state = userStates[userId];

                if (!state.IsFullNameReceived)
                {
                    // Получаем ФИО
                    state.FullName = message.Text;
                    state.IsFullNameReceived = true;
                    await client.SendMessage(chatId, "Какой у тебя никнейм?", cancellationToken: ct);
                }
                else
                {
                    // Получаем никнейм
                    var pilot = new Pilot
                    {
                        FullName = state.FullName,
                        Nickname = message.Text,
                        TelegramId = userId,
                        Channel = GetRandomChannel()
                    };

                    registeredPilots.Add(pilot);
                    userStates.Remove(userId); // Удаляем статус

                    PilotRegistered?.Invoke(pilot);

                    await client.SendMessage(
                        chatId,
                        $"Рад знакомству, {pilot.FullName}!\n" +
                        $"Твой ник: {pilot.Nickname}\n" +
                        $"Твой канал: {pilot.Channel}\n" +
                        "Жди команды \"Готов\".",
                        cancellationToken: ct
                    );
                }
            }
        }

        // Генерация случайного канала R1-R5 без повторений
        private string GetRandomChannel()
        {
            if (availableChannels.Count == 0) return null;
            var index = new Random().Next(availableChannels.Count);
            var channel = availableChannels[index];
            availableChannels.RemoveAt(index);
            return channel;
        }

        // Обработка ошибок
        private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка при работе с Telegram: {exception.Message}");
            return Task.CompletedTask;
        }





    }

}