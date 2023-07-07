using System;
using System.Net;
using System.Net.Http;
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
using Telegram.Bot.Types.Enums;
using System.Net.Http;
using System.IO;

namespace NewParser
{
    public class TgSent
    {
        public IArticleParser articleParser;
        public IMessageSender messageSender;

        public interface IArticleParser
        {
            List<Article> ParseArticle(string siteUrl, long chatId);
        }

        public interface IMessageSender
        {
            Task SendMessage(long chatId);
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
                // Список со ссылками на новости
                List<string> allNewsLinks = new List<string>();

                TelegramMessageSender messageSender = new TelegramMessageSender(bot, token);
                IWebDriver driver = new ChromeDriver();

                siteUrl = "https://nostroy.ru/company/news/";

                try
                {
                    List<Article> articles = new List<Article>();
                    driver.Navigate().GoToUrl(siteUrl);
                    Thread.Sleep(2000);

                    List<string> allLinks = new List<string>();

                    List<IWebElement> allLinksToNews = driver.FindElements(By.XPath("//div[contains(@class, 'm-info-item__title')]/a")).ToList();
                    foreach (var elm in allLinksToNews)
                    {
                        string lone = elm.GetAttribute("href");
                        allNewsLinks.Add(lone);
                        Console.WriteLine(elm.GetAttribute("href"));
                        allLinks.Add(elm.GetAttribute("href"));
                    }

                    for(int y = 0; y < allLinks.Count; y++)
                    {
                        string articleUrl = allLinks[0];
                        Console.WriteLine("Текущая ссылка: " + articleUrl);
                        driver.Navigate().GoToUrl(articleUrl);
                        allLinks.RemoveAt(0);
                        allLinks.Add(articleUrl);

                        // Собираем сссылки на страницы с новостями:
                        string newsTitle = driver.FindElement(By.XPath("//div[contains(@class, 'info-detail__name')]/h2")).Text;
                        List<IWebElement> newsBodyElements = driver.FindElements(By.XPath("//div[contains(@class, 'info-detail__content-text')]")).ToList();

                        Article article = null;

                        foreach (var newsBodyElement in newsBodyElements)
                        {
                            string newsBody = newsBodyElement.Text;

                            article = new Article
                            {
                                Title = newsTitle,
                                Body = newsBody,
                                Url = articleUrl
                            };

                            Console.WriteLine("Заголовок - " + article.Title);
                            Console.WriteLine("Тело - " + article.Body);
                            Console.WriteLine("Ссылка - " + article.Url);

                            articles.Add(article);
                        }
                    }

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

            public Task SendMessage(long chatId)
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

                    return SendMessageArticles(articles, chatId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при отправке сообщения: " + ex.Message);
                    return Task.CompletedTask;
                }
            }

            private async Task SendMessageArticles(List<Article> articles, long chatId)
            {
                try
                {
                    foreach (Article article in articles)
                    {
                        await bot.SendPhotoAsync(
                            chatId: new ChatId(chatId),
                            photo: InputFile.FromUri(article.ImgUrl),
                            parseMode: ParseMode.Html
                        );

                        await bot.SendTextMessageAsync(
                        chatId: new ChatId(chatId),
                        text: $"<b>{article.Title}</b>\n\n{article.Body}\n",
                        disableNotification: false,
                        parseMode: ParseMode.Html,
                        replyMarkup: new InlineKeyboardMarkup(
                            InlineKeyboardButton.WithUrl(
                                text: "Читать источник",
                                url: article.Url))
                        );
                    }
                    Thread.Sleep(3000);
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
        public string ImgUrl { get; set; }
    }

    public class ImageGetter
    {
        static async Task GetImg(string picUrl)
        {
            using(HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(picUrl);
                    response.EnsureSuccessStatusCode();

                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    // Сохранение картинки в файл
                    string filePath = Directory.GetCurrentDirectory() + @"\Img\image.jpg";
                    System.IO.File.WriteAllBytes(filePath, imageBytes);

                    Console.WriteLine("Картинка успешно скачана.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Ошибка при скачивании картинки: " + ex.Message);
                }
            }

        }
    }
}
