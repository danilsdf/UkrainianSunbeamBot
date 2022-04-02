using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace UkrainianSunbeamBot
{
    class Program
    {
        private static IMongoDatabase _db;
        private static readonly ReplyKeyboardMarkup _replyKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton>
        {
            new KeyboardButton("Отримати новину"),
        });

        private static void Main()
        {
            _replyKeyboard.ResizeKeyboard = true;
            var botClient = new TelegramBotClient("5105830409:AAHnrDJC1YhKBpr15HqJiCLI2NgI0TSgz5U"); // prod
            //var botClient = new TelegramBotClient("5139728542:AAFvCiXMRQY9LB_phRi3utDQjqLiaJRDFx8"); // dev
            using var cts = new CancellationTokenSource();

            var settings = MongoClientSettings.FromConnectionString("mongodb://admin2:x45JNZLhtrDXG2YD@ukrainiansunbeam-shard-00-00.0ndwc.mongodb.net:27017,ukrainiansunbeam-shard-00-01.0ndwc.mongodb.net:27017,ukrainiansunbeam-shard-00-02.0ndwc.mongodb.net:27017/news?ssl=true&replicaSet=atlas-hq4tm0-shard-0&authSource=admin&retryWrites=true&w=majority");
            
            var client = new MongoClient(settings);
            _db = client.GetDatabase("news");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new [] { UpdateType.Message, UpdateType.InlineQuery },
            };

            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cts.Token);

            Console.ReadKey();
        }

        private static async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update e,
            CancellationToken cancellationToken)
        {
            Console.WriteLine("The " + e.Message.Chat.FirstName + " Write: " + e);
            if (e?.Message?.Text == null) return;
            var message = e.Message;
            try
            {
                var users = _db.GetCollection<User>("users");
                var user = await users.Find(u => u.ChatId == message.Chat.Id).FirstOrDefaultAsync();
                if (user == default)
                {
                    user = new User
                    {
                        ChatId = message.Chat.Id,
                        UsedNews = new List<ObjectId>(),
                        Id = ObjectId.GenerateNewId()
                    };

                    await users.InsertOneAsync(user);
                }

                Console.WriteLine("The " + e.Message.Chat.FirstName + " Write: " + e.Message.Text);
                switch (message.Text)
                {
                    case "/start":
                        await botClient.SendTextMessageAsync(message.Chat,
                            $"Світ змінився - а воєнні конфлікти, як виявилося, ще існують." +
                            $"Тим гірше, що люди ведуться на фейки та втрачають бойовий дух - " +
                            $"коли навпаки потрібно вірити у краще." +
                            "\n\nЦей бот покликаний допомогти вам отримати власний промінь" +
                            " сонця у масі негативу.Одна кнопка - і в тебе актуальна гарна" +
                            " новина з фронту.Бо інакших новин і немає -" +
                            " просто є велика кількість інфомраційного бруду.",
                            replyMarkup: _replyKeyboard,
                            cancellationToken: cancellationToken);

                        break;
                    case "/get_news":
                    case "Отримати новину":
                        var newsCollection = _db.GetCollection<GoodNews>("goodNews");
                        var notUsedNews = await newsCollection
                            .Find(news => !user.UsedNews.Contains(news.Id))
                            .ToListAsync();

                        GoodNews nextNews;
                        if (notUsedNews.Count == 0)
                        {
                            var randomId = user.UsedNews[new Random().Next(user.UsedNews.Count)];
                            nextNews = await newsCollection.Find(news => news.Id == randomId).FirstOrDefaultAsync();
                            user.UsedNews = new List<ObjectId> { nextNews.Id };
                        }
                        else
                        {
                            nextNews = notUsedNews.Last();
                            user.UsedNews.Add(nextNews.Id);
                        }

                        await users.ReplaceOneAsync(doc => doc.ChatId == message.Chat.Id, user);

                        var textMessage = nextNews.Title + $"\n\n{nextNews.Body}";

                        if (!string.IsNullOrEmpty(nextNews.Source))
                        {
                            textMessage += $"\n\nДжерело: {nextNews.Source}";
                        }

                        var reactionButton = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData($"👍({nextNews.Likes})", "Cool"),},
                        });

                        await botClient.SendTextMessageAsync(message.Chat,
                            textMessage,
                            replyMarkup: _replyKeyboard,
                            cancellationToken: cancellationToken);
                        break;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}
