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
using System.Threading.Tasks;

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
            List<Article> ParseArticle(string siteUrl, long chatId);
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

            public List<Article> ParseArticle(string siteUrl, long chatId)
            {
                TelegramMessageSender messageSender = new TelegramMessageSender(bot, token);
                IWebDriver driver = new ChromeDriver();

                siteUrl = "https://nostroy.ru/company/news/";

                try
                {
                    driver.Navigate().GoToUrl(siteUrl);
                    Thread.Sleep(2000);

                    List<IWebElement> news = driver.FindElements(By.XPath("//div[contains(@class, 'm-info-item__title')]/a")).ToList();
                    List<IWebElement> newsBody = driver.FindElements(By.XPath("//div[contains(@class, 'm-info-item__text')]/p")).ToList();
                    List<IWebElement> articleUrls = driver.FindElements(By.XPath("//div[contains(@class, 'm-info-item__title')]/a")).ToList();

                    List<Article> articles = new List<Article>();

                    Article article = null;

                    for (int i = 0; i < news.Count; i++)
                    {
                        article = new Article
                        {
                            Title = news[i].Text,
                            Body = newsBody[i].Text,
                            Url = articleUrls[i].GetAttribute("href")
                        };
                        Console.WriteLine("Заголовок - " + article.Title);
                        Console.WriteLine("Тело - " + article.Body);
                        Console.WriteLine("Ссылка - " + article.Url);

                        articles.Add(article);
                    }
                    //messageSender.SendMessage(chatId);

                    return articles;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка парсинга статьи: " + ex.Message);
                    return null;
                }
                finally
                {
                    driver.Quit();
                    driver.Dispose();
                }
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
                try
                {
                    string siteUrl = String.Empty;

                    Article run = new Article();
                    run.Url = siteUrl;

                    IArticleParser articleParser = new WebsiteArticleParser(bot, token);
                    IMessageSender messageSender = new TelegramMessageSender(bot, token);

                    TgSent tgSent = new TgSent(articleParser, messageSender);

                    List<Article> articles = articleParser.ParseArticle(siteUrl, chatId);

                    await SendMessageArticles(articles, chatId);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Ошибка при отправке сообщения: " + ex.Message);
                }
            }

            private async Task SendMessageArticles(List<Article> articles, long chatId)
            {
                try
                {
                    foreach (Article article in articles)
                    {
                        await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Новость: \n Заголовок: {article.Title} \n Тело: {article.Body} \n",
                        disableNotification: true,
                        replyMarkup: new InlineKeyboardMarkup(
                            InlineKeyboardButton.WithUrl(
                                text: "Перейти на новость",
                                url: article.Url))
                        );
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
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
