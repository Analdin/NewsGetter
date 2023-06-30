using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using static NewParser.TgSent;

namespace NewParser
{
    public class Program
    {
        private string token = "5812061300:AAEmdV68-k0tN258qY56UZXrQb_6-qR6BYo";
        private TelegramBotClient bot;
        private long chatId = -1001710371108;

        static void Main(string[] args)
        {
            TelegramBotClient bot = new TelegramBotClient("5812061300:AAEmdV68-k0tN258qY56UZXrQb_6-qR6BYo");
            TgSent tgSent = new TgSent(new TgSent.WebsiteArticleParser(bot, "5812061300:AAEmdV68-k0tN258qY56UZXrQb_6-qR6BYo"), new TgSent.TelegramMessageSender(bot, "5812061300:AAEmdV68-k0tN258qY56UZXrQb_6-qR6BYo"));
            string siteUrl = "https://nostroy.ru/company/news/";
            tgSent.articleParser.ParseArticle(siteUrl, -1001710371108);
        }
    }
}
