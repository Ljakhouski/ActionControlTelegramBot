using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ActionControlTelegramBot
{
    /// <summary>
    /// Логика взаимодействия для Window.xaml
    /// </summary>
    /// 
    
    public partial class UiWindow : Window
    {
        public EventHandler TextEntered;
        public EventHandler ChatListClicked;
        public EventHandler TokenEditClicked;
        public EventHandler ShowUiChanged;
        public UiWindow()
        {
            InitializeComponent();
        }

        public void PrintMessage(string message)
        {
            BrushConverter bc = new BrushConverter();
            TextRange tr = new TextRange(this.textBox.Document.ContentEnd, this.textBox.Document.ContentEnd);
            //tr.Text = message;
            Dispatcher.Invoke(() => tr.Text = message);
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                bc.ConvertFromString("White"));
            }
            catch (FormatException) { }
        }

        public void PrintWarningMessage(string message)
        {
            BrushConverter bc = new BrushConverter();
            TextRange tr = new TextRange(this.textBox.Document.ContentEnd, this.textBox.Document.ContentEnd);
            //tr.Text = message;
            Dispatcher.Invoke(() => tr.Text = message);
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                bc.ConvertFromString("Red"));
            }
            catch (FormatException) { }
            //this.textBox.Sele
            //this.textBox.AppendText(message);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            this.PrintMessage('\n'+this.enterTextBox.Text);
            TextEntered?.Invoke(this.enterTextBox.Text, e);
            this.enterTextBox.Text = "";
            this.enterTextGrid.Visibility = Visibility.Hidden;
            
        }

        public void TextEnteringEnable()
        {
            this.enterTextGrid.Visibility = Visibility.Visible;
        }

        private void enterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Button_Click(sender, e);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + "info.json" + " && exit");
            ProcessInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = true;
            ProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;

            Process = Process.Start(ProcessInfo);
            //Process.WaitForExit();
        }

        private void ChatListButton_Click(object sender, RoutedEventArgs e)
        {
            ChatListClicked?.Invoke(sender, e);
        }

        private void EditTokenButton_Clicked(object sender, RoutedEventArgs e)
        {
            TokenEditClicked?.Invoke(sender, e);
        }

        private void ShowUiChecked(object sender, RoutedEventArgs e)
        {
            ShowUiChanged?.Invoke((sender as CheckBox).IsChecked, e);
        }
    }
}
