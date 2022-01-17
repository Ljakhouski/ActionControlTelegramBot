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
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Threading;
using System.Windows.Forms;

namespace ActionControlTelegramBot
{
    static class Bot
    {
        public static async Task Start(Info info)
        {
            Program.Bot = new TelegramBotClient(info.Token);

            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },   // receive all update types
            };
            Program.Bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token);

            var me = await Program.Bot.GetMeAsync();

            /*Ui.Dispatcher.Invoke(() =>
            {
                Ui.PrintMessage("Bot started...");
            },;*/

            Program.Ui.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                       new Action(() => { Program.Ui.PrintMessage("Bot started...\n"); }));

            foreach (UserInfo i in info.Users)
                await Program.Bot.SendTextMessageAsync(
                chatId: i.ChatId,
                text: "Computer started!\n" + "time: " + Program.CurrentLog.startTime.ToString(),
                cancellationToken: cts.Token);


        }

        async static Task HandleUpdateAsync(ITelegramBotClient BotClient, Update update, CancellationToken cancellationToken)
        {
            if (Program.Info.IgnoreAll)
                return;

            if (update.Type != UpdateType.Message)
                return;

            if (update.Message!.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            if (!Program.Info.ContainsUser(chatId))
            {
                Program.Info.Users.Add(new UserInfo() { ChatId = chatId, LastName = update.Message.Chat.LastName, UzerName = update.Message.Chat.Username });
                System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(Program.Info), Encoding.UTF8);

                await BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Hi new user! Computer is running now. Send /help to get a list of commands",
                cancellationToken: cancellationToken);

                return;
            }

            if (messageText == "/apps")
            {
                var l = getWindowProcesses();

                string result = "Running apps: \n\n";
                foreach (Process p in l)
                    result += "  " + p.ProcessName + "\n";

                await BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: result,
                cancellationToken: cancellationToken);

                return;
            }
            else if (messageText == "/screen")
            {

                Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);
                    bmp.Save("last_screenshot.png");  // saves the image
                    MemoryStream memoryStream = new MemoryStream();
                    bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    //using () 
                    var stream = System.IO.File.Open("last_screenshot.png", FileMode.Open);

                    //stream.Write(memoryStream.GetBuffer());
                    //memoryStream.CopyTo(stream);
                    var rep = await BotClient.SendPhotoAsync(chatId, stream, "screen_" + DateTime.Now.ToString());



                    //var v = await BotClient.SendPhotoAsync(chatId, memoryStream, "screen_"+DateTime.Now.ToString(), cancellationToken: cancellationToken);
                }
                return;
            }
            else if (messageText == "/showUi")
            {
                Program.Info.ShowWindow = true;
                System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(Program.Info), Encoding.UTF8);
                Program.NeedToShow = true;


                Telegram.Bot.Types.Message sentMessage = await BotClient.SendTextMessageAsync(
                 chatId: chatId,
                 text: "Ui is shown",
                 cancellationToken: cancellationToken);
            }
            else if (messageText == "/hideUi")
            {
                Program.Info.ShowWindow = false;
                System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(Program.Info), Encoding.UTF8);
                Program.Ui.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                       new Action(() => { Program.Ui.Close(); }));

                Telegram.Bot.Types.Message sentMessage = await BotClient.SendTextMessageAsync(
                 chatId: chatId,
                 text: "Ui is hidden",
                 cancellationToken: cancellationToken);
            }
            else if (messageText == "/turnoff")
            {
                Telegram.Bot.Types.Message sentMessage = await BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Turn off the computer... ",
                cancellationToken: cancellationToken);

                Process.Start("shutdown", "/s /t 0");
            }
            else if (messageText == "/reboot")
            {
                Telegram.Bot.Types.Message sentMessage = await BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Rebooting the computer... ",
                cancellationToken: cancellationToken);

                Process.Start("shutdown", "/r /t 0");
            }
            else if (messageText == "/log")
            {
                await BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: "nothing...",
                cancellationToken: cancellationToken);

                return;
            }
            else if (messageText == "/mute")
            {
                foreach (UserInfo i in Program.Info.Users)
                    if (i.ChatId == chatId)
                    {
                        i.RecieveLogs = false;
                        System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(Program.Info), Encoding.UTF8);
                    }
            }
            else if (messageText == "/unmute")
            {
                foreach (UserInfo i in Program.Info.Users)
                    if (i.ChatId == chatId)
                    {
                        i.RecieveLogs = true;
                        System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(Program.Info), Encoding.UTF8);
                    }
            }
            else if (messageText == "/help")
            {
                Telegram.Bot.Types.Message sentMessage = await BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Command list: \n /mute \n /unmute \n /log \n /turnoff \n /reboot \n /showUi \n /hideUi \n /screen \n /apps",
                cancellationToken: cancellationToken);
            }
            else
            {
                Telegram.Bot.Types.Message sentMessage = await BotClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Computer is running now",
                    cancellationToken: cancellationToken);
            }
        }

        static Task HandleErrorAsync(ITelegramBotClient BotClient, Exception exception, CancellationToken cancellationToken)
        {
            //return Task.CompletedTask; 
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            //Console.WriteLine(ErrorMessage);
            Program.Ui.Dispatcher.Invoke(() => { Program.Ui.PrintWarningMessage(errorMessage); });
            return Task.CompletedTask;
        }
        static List<Process> getWindowProcesses()
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
