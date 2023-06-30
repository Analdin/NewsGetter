using System;
using System.Net;
using System.Security.Cryptography;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace NewParser
{
    public class TgSent
    {
        private string token = "5812061300:AAEmdV68-k0tN258qY56UZXrQb_6-qR6BYo";
        private TelegramBotClient bot;
        private long chatId = -1001710371108;
        private Random rnd = new Random();

        public IArticleParser articleParser;
        public IMessageSender messageSender;

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
                IWebDriver driver = new ChromeDriver();

                using (WebClient client = new WebClient())
                {
                    try
                    {
                        //client.Encoding = Encoding.UTF8;
                        //string html = client.DownloadString(siteUrl);
                        Console.WriteLine($"Отправили запрос к странице {siteUrl}");
                        //Console.WriteLine($"{html}");

                        driver.Navigate().GoToUrl(siteUrl);
                        Thread.Sleep(2000);

                        // Здесь происходит парсинг статьи из полученного HTML
                        //MatchCollection news = Regex.Matches(siteUrl, @"(?<=class=""dark-color"">\ ).*(?=</a>\ )");
                        //MatchCollection newsBody = Regex.Matches(siteUrl, @"(?<=text-align:\ justify;"">)[\w\W]*?(?=</p>)");
                        //MatchCollection articleUrls = Regex.Matches(siteUrl, @"(?<=<a\ href="").*?(?=""\ class=""dark-color)");

                        List<IWebElement> news = driver.FindElements(By.XPath("//div[contains(@class, 'm-info-item__title')]/a")).ToList();
                        List<IWebElement> newsBody = driver.FindElements(By.XPath("//div[contains(@class, 'm-info-item__text')]/p")).ToList();
                        List<IWebElement> articleUrls = driver.FindElements(By.XPath("//div[contains(@class, 'm-info-item__title')]/a")).ToList();

                        for(int i = 0; i < news.Count; i++)
                        {
                            // Создаем объект Article с заполненными данными
                            Article article = new Article
                            {
                                Title = news[i].Text,
                                Body = newsBody[i].Text,
                                Url = articleUrls[i].Text
                            };
                            Console.WriteLine("Заголовок - " + article.Title);
                            Console.WriteLine("Тело - " + article.Body);
                            Console.WriteLine("Ссылка - " + article.Url);

                            // Отправляем сообщение в телеграм
                            messageSender.SendMessage(chatId);
                        }

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
