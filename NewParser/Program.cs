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
        public static string token = "5812061300:AAEmdV68-k0tN258qY56UZXrQb_6-qR6BYo";
        public static long chatId = -1001717429781;
        //1001717429781
        //1001710371108

        static void Main(string[] args)
        {
            RunBotAsync().GetAwaiter().GetResult();
        }
        static async Task RunBotAsync()
        {
            TelegramBotClient bot = new TelegramBotClient(token);

            var me = await bot.GetMeAsync();
            if (me != null)
            {
                Console.WriteLine($"Бот подключен: {me.Username}");
            }

            TgSent tgSent = new TgSent(new WebsiteArticleParser(bot, token), new TelegramMessageSender(bot, token));
            tgSent.messageSender.SendMessage(chatId);
        }
    }
}
