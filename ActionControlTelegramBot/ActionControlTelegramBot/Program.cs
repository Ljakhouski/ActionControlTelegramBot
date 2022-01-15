using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
//using System.Windows.Forms;

namespace ActionControlTelegramBot
{
    class UserInfo
    {
        public long ChatId;
        public bool RecieveLogs = true;
    }
    class Info
    {
        public string Token;
        public List<UserInfo> Users = new List<UserInfo>();
        public bool ContainsUser(long id)
        {
            foreach (UserInfo i in Users)
            {
                if (i.ChatId == id)
                    return true;
            }
            return false;
        }
    }
    class Program
    {
        static Info info;
        static Log currentLog;
        private static TelegramBotClient? bot;
        static async Task Main(string[] args)
        {
            if (System.IO.File.Exists("info.json"))
            {
                string content = System.IO.File.ReadAllText("info.json", Encoding.UTF8);
                info = JsonConvert.DeserializeObject<Info>(content);
            }
            else
            {
                Console.WriteLine("info.json was not found");
                Console.WriteLine("let's generate info.json...");
                Console.WriteLine("paste this token from telegram-bot: ");
                string token = Console.ReadLine();
                try
                {
                    info = new Info() { Token = token };
                    System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(info), Encoding.UTF8);
                    Console.WriteLine("Successfully\n.\n.\n.\n");
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            }

            currentLog = new Log() { startTime = DateTime.Now };
            await Start(info);

        }
        public static async Task Start(Info info)
        {
            bot = new TelegramBotClient(info.Token);
            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };
            bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token);

            var me = await bot.GetMeAsync();
            Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

            foreach (UserInfo i in info.Users)
                await bot.SendTextMessageAsync(
                chatId: i.ChatId,
                text: "Computer started!\n" + "time: "+currentLog.startTime.ToString(),
                cancellationToken: cts.Token);

            Console.ReadKey();
        }

        async static Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;

            if (update.Message!.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            if (!info.ContainsUser(chatId))
            {
                info.Users.Add(new UserInfo() { ChatId = chatId });
                System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(info), Encoding.UTF8);

                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Hi new user! Computer is running now. Send /apps for check running apps",
                cancellationToken: cancellationToken);

                return;
            }

            if (messageText == "/apps")
            {
                var l = GetWindowProcesses();

                string result = "Running apps: \n\n";
                foreach (Process p in l)
                    result += "  "+p.ProcessName + "\n";

                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: result,
                cancellationToken: cancellationToken);

                return;
            }
            else if (messageText == "/screen")
            {
               // Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
               // using (Graphics g = Graphics.FromImage(bmp))
               // {
               //     g.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);
               //     bmp.Save("screenshot.png");  // saves the image
               // }
               //
               // await botClient.SendTextMessageAsync(
               // chatId: chatId,
               // text: result,
               // cancellationToken: cancellationToken);
               //
               // return;
            }
            else if (messageText == "/log")
            {
                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "nothing...",
                cancellationToken: cancellationToken);

                return;
            }
            else if (messageText == "/mute")
            {
                foreach (UserInfo i in info.Users)
                    if (i.ChatId == chatId)
                    {
                        i.RecieveLogs = false;
                        System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(info), Encoding.UTF8);
                    }
            }
            else if (messageText == "/unmute")
            {
                foreach (UserInfo i in info.Users)
                    if (i.ChatId == chatId)
                    {
                        i.RecieveLogs = true;
                        System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(info), Encoding.UTF8);
                    }
            }

            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Computer is running now",
                cancellationToken: cancellationToken);
        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        static List<Process> GetWindowProcesses()
        {
            var l = new List<Process>();
            foreach (var proc in Process.GetProcesses())
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                {
                    l.Add(proc);
                }
            }
            return l;
        }
    }
}
