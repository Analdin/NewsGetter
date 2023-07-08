using System;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

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
            Task SendMessage(long chatId, List<Article> articles);
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

                try
                {
                    List<Article> articles = new List<Article>();
                    driver.Navigate().GoToUrl(siteUrl);
                    Thread.Sleep(2000);

                    List<string> allLinks = new List<string>();

                    List<IWebElement> allLinksToNews = new List<IWebElement>();

                    if (siteUrl.Contains("gosnadzor"))
                    {
                        allLinksToNews = driver.FindElements(By.XPath("//div[contains(@class, 'news-list')]/p/a")).ToList();
                    }
                    else
                    {
                        allLinksToNews = driver.FindElements(By.XPath("//div[contains(@class, 'm-info-item__title')]/a")).ToList();
                    }
                    
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

                        string imgUrl = String.Empty;

                        Thread.Sleep(3000);

                        // Собираем сссылки на страницы с новостями:
                        string newsTitle = String.Empty;

                        ((IJavaScriptExecutor)driver).ExecuteScript("window.stop();");

                        if (articleUrl.Contains("gosnadzor"))
                        {
                            newsTitle = driver.FindElement(By.XPath("//h1[contains(@id, 'page_title')]")).Text;
                        }
                        else
                        {
                            newsTitle = driver.FindElement(By.XPath("//div[contains(@class, 'info-detail__name')]/h2")).Text;
                        }

                        string xpath = String.Empty;

                        if (articleUrl.Contains("gosnadzor"))
                        {
                            xpath = "//div[contains(@class, 'news-detail')]/p";
                        }
                        else
                        {
                            xpath = "//div[contains(@class, 'info-detail__content-text')]/p";
                        }

                        TextAlign align = new TextAlign();
                        List<string> FullText = align.TextGlu(driver, xpath);

                        //List<IWebElement> newsBodyElements = driver.FindElements(By.XPath("//div[contains(@class, 'info-detail__content-text')]")).ToList();
                        List<IWebElement> urlGetter = driver.FindElements(By.XPath("//div[contains(@class, 'info-detail__image-block')]/a/img")).ToList();

                        if(urlGetter.Count > 0)
                        {
                            imgUrl = urlGetter.FirstOrDefault().GetAttribute("src");
                        }

                        Article article = null;

                        foreach (var newsBodyElement in FullText)
                        {
                            //string newsBody = newsBodyElement.Text;
                            string newsBody = newsBodyElement;

                            if (newsBody.Length > 500 && !String.IsNullOrWhiteSpace(imgUrl))
                            {
                                newsBody = newsBody.Substring(0, 500);
                            }
                            else if (newsBody.Length > 4000 && String.IsNullOrWhiteSpace(imgUrl))
                            {
                                newsBody = newsBody.Substring(0, 4000);
                            }

                            article = new Article
                            {
                                Title = newsTitle,
                                Body = newsBody,
                                Url = articleUrl,
                                ImgUrl = imgUrl
                            };

                            Console.WriteLine("Заголовок - " + article.Title);
                            Console.WriteLine("Тело - " + article.Body);
                            Console.WriteLine("Ссылка - " + article.Url);
                            Console.WriteLine("Ссылка на картинку - " + article.ImgUrl);

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

            public Task SendMessage(long chatId, List<Article> articles)
            {
                try
                {
                    string siteUrl = String.Empty;

                    Article run = new Article();
                    run.Url = siteUrl;

                    IArticleParser articleParser = new WebsiteArticleParser(bot, token);
                    IMessageSender messageSender = new TelegramMessageSender(bot, token);

                    TgSent tgSent = new TgSent(articleParser, messageSender);

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
                        if (String.IsNullOrWhiteSpace(article.Title))
                        {
                            article.Title = "***";
                        }

                        if (!String.IsNullOrWhiteSpace(article.ImgUrl))
                        {
                            await bot.SendPhotoAsync(
                                chatId: new ChatId(chatId),
                                photo: InputFile.FromUri(article.ImgUrl),
                                caption: $"<b>{article.Title}</b>\n\n{article.Body}\n\n<a href=\"{article.Url}\">Читать источник</a>",
                                parseMode: ParseMode.Html
                                //replyMarkup: new InlineKeyboardMarkup(
                                //InlineKeyboardButton.WithUrl(
                                    //text: "Читать источник",
                                    //url: article.Url))
                            );
                        }
                        else
                        {
                            await bot.SendTextMessageAsync(
                            chatId: new ChatId(chatId),
                            text: $"<b>{article.Title}</b>\n\n{article.Body}\n\n<a href=\"{article.Url}\">Читать источник</a>",
                            disableNotification: false,
                            parseMode: ParseMode.Html
                            //replyMarkup: new InlineKeyboardMarkup(
                                //InlineKeyboardButton.WithUrl(
                                    //text: "Читать источник",
                                    //url: article.Url))
                            );
                        }
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

    public class TextAlign
    {
        public List<string> TextGlu(IWebDriver driver, string xpath)
        {
            int maxLength = 700;

            IReadOnlyCollection<IWebElement> paragraphElements = driver.FindElements(By.XPath(xpath));
            List<string> paragraphTexts = new List<string>();
            foreach (IWebElement paragraphElement in paragraphElements)
            {
                string paragraphText = paragraphElement.Text.Trim();
                paragraphTexts.Add(paragraphText);
            }
            string combinedText = string.Join(" ",paragraphTexts);
            paragraphTexts.Clear();
            paragraphTexts.Add(combinedText);

            // Раздбиваем текст на абзацы
            //paragraphTexts.Clear();
            //paragraphTexts.Add(combinedText);

            //string[] sentences = combinedText.Split('.');
            //StringBuilder paragraph = new StringBuilder();

            //foreach (string sentence in sentences)
            //{
            //    if(paragraph.Length + sentence.Length + 1 <= maxLength)
            //    {
            //        paragraph.Append(sentence.Trim());
            //    }
            //    else
            //    {
            //        paragraphTexts.Add(paragraph.ToString().Trim()); // Завершаем текущий абзац и добавляем его в список
            //        paragraph.Clear(); // Очищаем текущий абзац
            //        paragraph.Append(sentence.Trim() + ". ");
            //    }
            //}

            //if(paragraph.Length > 0)
            //{
            //    paragraphTexts.Add(paragraph.ToString().Trim());
            //}

            return paragraphTexts;
        }
    }
}
