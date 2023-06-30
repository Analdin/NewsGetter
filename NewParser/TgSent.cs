using System;
using System.Net;
using System.Security.Cryptography;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using System.Security.Policy;

namespace NewParser
{
    public class TgSent
    {
        private string token = "5812061300:AAEmdV68-k0tN258qY56UZXrQb_6-qR6BYo";
        private TelegramBotClient bot;
        private long chatId = -1001710371108;
        private Random rnd = new Random();

        private IArticleParser articleParser;
        private IMessageSender messageSender;

        public interface IArticleParser
        {
            Article ParseArticle(string siteUrl, long chatId);
        }

        public interface IMessageSender
        {
            void SendMessage(long chatId);
        }

        public TgSent(IArticleParser articleParser, IMessageSender messageSender)
        {
            this.articleParser = articleParser;
            this.messageSender = messageSender;
        }

        public class WebsiteArticleParser : IArticleParser
        {
            private TelegramBotClient bot;
            private string token;

            public WebsiteArticleParser(TelegramBotClient bot, string token)
            {
                this.bot = bot;
                this.token = token;
            }

            public Article ParseArticle(string siteUrl, long chatId)
            {
                TelegramMessageSender messageSender = new TelegramMessageSender(bot, token);

                using (WebClient client = new WebClient())
                {
                    try
                    {
                        string html = client.DownloadString(siteUrl);
                        Console.WriteLine($"Отправили запрос к странице {siteUrl}");

                        // Здесь происходит парсинг статьи из полученного HTML

                        // Создаем объект Article с заполненными данными
                        Article article = new Article
                        {
                            Title = "Заголовок статьи",
                            Body = "Тело статьи",
                            Url = siteUrl
                        };

                        // Отправляем сообщение в телеграм
                        messageSender.SendMessage(chatId);

                        return article;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка парсинга статьи: " + ex.Message);
                    }
                }

                return null;
            }
        }

        public class TelegramMessageSender : IMessageSender
        {
            private TelegramBotClient bot;
            private string token;

            public TelegramMessageSender(TelegramBotClient bot, string token)
            {
                this.bot = bot;
                this.token = token;
            }

            public async void SendMessage(long chatId)
            {
                Article run = new Article();

                await bot.SendTextMessageAsync(
                    chatId: new ChatId(chatId),
                    text: $"Новость: \n Заголовок: {run.Title} \n Тело: {run.Body} \n",
                    disableNotification: true,
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithUrl(
                            text: "Перейти на новость",
                            url: run.Url))
                );
            }
        }
    }

    public class Article
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string Url { get; set; }
    }
}
