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
    class UserInfo
    {
        public long ChatId;
        public bool RecieveLogs = true;
    }
    class Info
    {
        public string Token;
        public List<UserInfo> Users = new List<UserInfo>();
        public bool ShowWindow = true;
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

        private static UIWindow ui;

        enum Mode
        {
            TokenEntering,
            None
        }
        static Mode mode = Mode.None;
        [STAThread]
        static /*async Task*/ void Main(string[] args)
        {
            //Thread viewThread = new Thread(() =>
            //{
            
            currentLog = new Log() { startTime = DateTime.Now };
            ui = new UIWindow();
            ui.TextEntered += textEntered_Clicked;
            //});
        
            //viewThread.SetApartmentState(ApartmentState.STA);
            //viewThread.Start();
            
            
            if (System.IO.File.Exists("info.json"))
            {
                string content = System.IO.File.ReadAllText("info.json", Encoding.UTF8);
                info = JsonConvert.DeserializeObject<Info>(content);

                currentLog = new Log() { startTime = DateTime.Now };
                Task.Run(async () =>
                {
                    await Start(info);
                }
                ).GetAwaiter().GetResult();
                

                if (info.ShowWindow)
                {
                    
                     ui.ShowDialog();

                    
                }
            }
            else
            {
                
                
                ui.PrintWarningMessage("info.json was not found\n");
                ui.PrintWarningMessage("let's generate info.json...\n");
                ui.PrintWarningMessage("paste this token from telegram-bot: ");
                mode = Mode.TokenEntering;
                ui.TextEnteringEnable();
                ui.ShowDialog();

                Task.Run(async () =>
                {
                    await Start(info);
                }
                ).GetAwaiter().GetResult();
                                           
                                           

                // viewThread3.SetApartmentState(ApartmentState.STA);
                // viewThread3.Start();


                /*Dispatcher.Invoke(() =>
                {
                    ui.PrintWarningMessage("info.json was not found\n");
                    ui.PrintWarningMessage("let's generate info.json...\n");
                    ui.PrintWarningMessage("paste this token from telegram-bot: ");
                    mode = Mode.TokenEntering;
                    ui.TextEnteringEnable();
                }
                );*/
            }





            //Keyboard. keys = Keyboard.GetState(); while (!keys.IsKeyDown(Keys.P))
            Console.Read();

            while (true) //Console.Read();
                Task.Delay(1000);
        }

        private static void textEntered_Clicked(object sender, EventArgs e)
        {
            switch (mode)
            {
                case Mode.TokenEntering:
                {
                    try
                    {
                        mode = Mode.None;
                        info = new Info() { Token = (string) sender };
                        System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(info), Encoding.UTF8);
                        ui.PrintMessage("Successfully\n.\n.\n.\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                        
                    break;
                }
                    
                default:
                    break;
            }
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
            
            /*ui.Dispatcher.Invoke(() =>
            {
                ui.PrintMessage("Bot started...");
            },;*/

            ui.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                       new Action(() => { ui.PrintMessage("Bot started..."); }));

            foreach (UserInfo i in info.Users)
                await bot.SendTextMessageAsync(
                chatId: i.ChatId,
                text: "Computer started!\n" + "time: "+currentLog.startTime.ToString(),
                cancellationToken: cts.Token);

            
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
                    var rep = await botClient.SendPhotoAsync(chatId, stream, "screen_" + DateTime.Now.ToString());
                
                

                //var v = await botClient.SendPhotoAsync(chatId, memoryStream, "screen_"+DateTime.Now.ToString(), cancellationToken: cancellationToken);
                }
                return;
            }
            else if (messageText == "/showui")
            {
                info.ShowWindow = true;
                System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(info), Encoding.UTF8);

                Thread newWindowThread = new Thread(new ThreadStart(()=> { SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher)); ui.ShowDialog(); System.Windows.Threading.Dispatcher.Run(); }));
                newWindowThread.SetApartmentState(ApartmentState.STA);
                newWindowThread.IsBackground = true;
                newWindowThread.Start();

                //Thread viewThread = new Thread(() =>
                //{
                //    
                //    ui.ContextMenu.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                //    new Action(() => { ui.ShowDialog(); }));
                //});
                //viewThread.SetApartmentState(ApartmentState.STA);
                //viewThread.Start();

                Telegram.Bot.Types.Message sentMessage = await botClient.SendTextMessageAsync(
                 chatId: chatId,
                 text: "UI is shown",
                 cancellationToken: cancellationToken);
            }
            else if (messageText == "/hideui")
            {
                info.ShowWindow = false;
                System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(info), Encoding.UTF8);
                ui.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                       new Action(() => { ui.Close(); }));

                Telegram.Bot.Types.Message sentMessage = await botClient.SendTextMessageAsync(
                 chatId: chatId,
                 text: "UI is hidden",
                 cancellationToken: cancellationToken);
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
            else
            { 
            Telegram.Bot.Types.Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Computer is running now",
                cancellationToken: cancellationToken);
            }
        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            //Console.WriteLine(ErrorMessage);
            ui.Dispatcher.Invoke(() => { ui.PrintWarningMessage(errorMessage); });
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
