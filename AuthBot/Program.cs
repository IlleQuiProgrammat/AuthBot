using System;
using System.Threading.Tasks;
using AuthBot.Models;

namespace AuthBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");
            var bot = new Bot(BotConfig.LoadBotConfig("./config.json"));
            await bot.Run();
            Console.WriteLine("Bye.");
        }
    }
}
