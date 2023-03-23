using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ActionControlTelegramBot
{
   
    class Program
    {
        public static Info Info;
        public static Log CurrentLog;
        public static Telegram.Bot.TelegramBotClient? Bot;

        public static UiWindow Ui;
        public static bool NeedToShow = false;

        public static Bsod BsodUi;

        [STAThread]
        static void Main(string[] args)
        {
            CurrentLog = new Log() { startTime = DateTime.Now };
            makeUi();
            makeInfo();
            mainLoop();
        }

        static void makeInfo()
        {
            if (System.IO.File.Exists("Info.json"))
            {
                string content = System.IO.File.ReadAllText("info.json", Encoding.UTF8);
                Info = JsonConvert.DeserializeObject<Info>(content);

                CurrentLog = new Log() { startTime = DateTime.Now };
                Task.Run(async () =>
                {
                    await ActionControlTelegramBot.Bot.Start(Info);
                }
                ).GetAwaiter().GetResult();


                if (Info.ShowWindow)
                {
                    Ui.ShowDialog();
                }
            }
            else
            {
                Ui.PrintWarningMessage("info.json was not found\n");
                Ui.PrintWarningMessage("let's generate info.json...\n");
                Info = new Info();
                Ui.ShowDialog();
                editToken(null, null); 
                //...
            }
        }
        private static void makeUi()
        {
            Ui = new UiWindow();
            Ui.TextEntered +=      tokenEntered;
            Ui.ChatListClicked +=  printChatList;
            Ui.TokenEditClicked += editToken;
            Ui.ShowUiChanged +=    showUiChanged;
        }

        private static void mainLoop()
        {
            while (true)
            {
                Thread.Sleep(500);
                if (NeedToShow)
                {
                    NeedToShow = false;
                    makeUi();
                    Ui.ShowDialog();
                }

                if (Program.Info.ShowJokeWindow)
                {
                    BsodUi = new Bsod();
                    Program.Info.ShowJokeWindow = false;
                    BsodUi.ShowDialog();
                }
            }
        }
        private static void showUiChanged(object sender, EventArgs e)
        {
            Info.ShowWindow = (bool)sender;
            System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(Info), Encoding.UTF8);
            Ui.PrintMessage((bool)sender? "\nShowing Ui Enabled\n" : "\nShowing Ui Disabled\n");
        }

        private static void printChatList(object sender, EventArgs e)
        {
            if (Info != null)
                foreach (UserInfo i in Info.Users)
                {
                    Ui.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                           new Action(() => { Ui.PrintMessage("\nUzer: \"" + i.UzerName+ "\"\nName: "+i.LastName+"\nChat Id: "+i.ChatId.ToString()+"\n"); }));
                }
        }

        private static void tokenEntered(object sender, EventArgs e)
        {
            try
            {
                bool isNewBot = Info.Token == null || Info.Token == "" ? true : false;
                Info.Token = (string)sender;
                System.IO.File.WriteAllText("info.json", JsonConvert.SerializeObject(Info), Encoding.UTF8);
                Ui.PrintMessage("\nSuccessfully\n");

                if (isNewBot == false)
                    Task.Run(async () =>
                    {
                        await ActionControlTelegramBot.Bot.Start(Info);
                    }
                    ).GetAwaiter().GetResult();

            }
            catch (Exception ex)
            {
                Ui.PrintWarningMessage(ex.Message);
            }
                  
        }

        
        static void editToken(object sender, EventArgs e)
        {
            try
            {
                Ui.PrintWarningMessage("paste token from telegram-Bot here: \n");
                Ui.TextEnteringEnable();
                if (!Ui.IsActive)
                    Ui.ShowDialog();
                Ui.TextEntered += tokenEntered; // continue with this method after token entering 
            }
            catch(Exception e_)
            {

            }
        }
        
    }
}
