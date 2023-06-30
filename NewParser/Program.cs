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
        private static string token = "5812061300:AAEmdV68-k0tN258qY56UZXrQb_6-qR6BYo";
        private static long chatId = -1001717429781;
        //1001717429781
        //1001710371108

        static void Main(string[] args)
        {
            TelegramBotClient bot = new TelegramBotClient(token);
            TgSent tgSent = new TgSent(new WebsiteArticleParser(bot, token), new TelegramMessageSender(bot, token));
            tgSent.messageSender.SendMessage(chatId);
        }
    }
}
