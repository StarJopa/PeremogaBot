using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    private static ITelegramBotClient botClient;
    private static DuckDuckGoSearchService searchService = new DuckDuckGoSearchService();

    static async Task Main()
    {
        botClient = new TelegramBotClient("7230033218:AAGfybAr5A-qxSCPakRUvX_FF2o_S515xkU"); 

        using var cts = new CancellationTokenSource();
        var receiverOptions = new Telegram.Bot.Polling.ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Text != null)
        {
            var message = update.Message;

            // Приветственное сообщение при запуске бота
            if (message.Text.ToLower().Contains("/start"))
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Нажмите 'Привет', чтобы начать.",
                    cancellationToken: cancellationToken
                );

                // Отправляем клавиатуру с кнопкой "Привет"
                await SendGreetingKeyboard(message.Chat.Id, cancellationToken);
                return;
            }

            // Если пользователь нажал кнопку "Привет"
            if (message.Text.ToLower() == "привет")
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Привет! Что бы вы хотели спросить?",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Исключаем ответы "Да" и "Нет" из поисковых запросов
            if (message.Text.ToLower() == "да")
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Что бы вы хотели спросить?",
                    cancellationToken: cancellationToken
                );
                return;
            }

            if (message.Text.ToLower() == "нет")
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Спасибо, до свидания!",
                    cancellationToken: cancellationToken
                );

                // После прощания снова отправляем запрос на приветствие
                await SendGreetingKeyboard(message.Chat.Id, cancellationToken);
                return;
            }

            // Обработка запросов, связанных с обучением
            string searchResult = await ProcessEducationalRequest(message.Text);

            // Отправляем результаты пользователю
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: searchResult,
                cancellationToken: cancellationToken
            );
        }
    }

    // Метод для обработки запросов, связанных с обучением
    private static async Task<string> ProcessEducationalRequest(string query)
    {
        // Предустановленные ответы для типичных вопросов
        var predefinedAnswers = new Dictionary<string, string>
        {
            { "как измерить длину окружности", "Для того чтобы измерить длину окружности, используйте формулу: C = 2πr, где C — длина окружности, r — радиус окружности, а π — математическая константа, примерно равная 3,1416." },
            { "теорема пифагора", "Теорема Пифагора утверждает: в прямоугольном треугольнике квадрат гипотенузы равен сумме квадратов катетов. Формула: c² = a² + b²." },
            { "площадь круга", "Площадь круга можно найти по формуле: S = πr², где S — площадь, r — радиус круга, а π — константа, равная примерно 3,1416." }
        };

        // Проверяем, есть ли предустановленный ответ на запрос
        foreach (var predefinedAnswer in predefinedAnswers)
        {
            if (query.ToLower().Contains(predefinedAnswer.Key))
            {
                return predefinedAnswer.Value;
            }
        }

        // Используем DuckDuckGo для поиска по запросу
        string searchResult = await searchService.SearchDuckDuckGoAsync(query);

        // Если DuckDuckGo не дал результатов
        if (string.IsNullOrEmpty(searchResult) || searchResult.Contains("Ничего не найдено"))
        {
            return "Пожалуйста, задавайте вопросы только на темы, связанные с обучением.";
        }

        return searchResult;
    }

    // Метод для отправки клавиатуры с кнопкой "Привет"
    private static async Task SendGreetingKeyboard(long chatId, CancellationToken cancellationToken)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Привет" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Нажмите 'Привет', чтобы начать.",
            replyMarkup: replyKeyboard,
            cancellationToken: cancellationToken
        );
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}
