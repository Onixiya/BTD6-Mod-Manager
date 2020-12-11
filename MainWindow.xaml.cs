﻿using BTD_Backend;
using BTD_Backend.Game;
using BTD_Backend.Persistence;
using BTD_Backend.Web;
using BTD6_Mod_Manager.Classes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BTD6_Mod_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool doingWork = false;
        public static string workType = "";
        public static MainWindow instance;
        public MainWindow()
        {
            InitializeComponent();
            instance = this;

            SessionData.CurrentGame = GameType.BTD6;
            Log.MessageLogged += Log_MessageLogged;

            Log.Output("Program initializing...");
            Startup();
        }

        private void Startup()
        {
            UserData.UserDataLoaded += UserData_UserDataLoaded;
            TempSettings.Instance.LoadSettings();
            TempSettings.Instance.SaveSettings();

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string[] split = version.Split('.');
            
            if (split.Length - 1 > 2)
                version = version.Remove(version.Length - 2, 2);

            Version_TextBlock.Text = "Version " + version;
        }

        private void OnFinishedLoading()
        {
            Log.Output("Welcome to BTD6 Mod Manager!");
            
            string tdloaderDir = Environment.CurrentDirectory + "\\";
            UserData.MainProgramExePath = Environment.CurrentDirectory + "\\BTD6 Mod Manager.exe";
            UserData.MainProgramName = "BTD6 Mod Manager";
            UserData.MainSettingsDir = tdloaderDir;
            UserData.UserDataFilePath = tdloaderDir + "\\userdata.json";
            var game = GameInfo.GetGame(SessionData.CurrentGame);
            string btd6ExePath = game.GameDir + "\\" + game.EXEName;
            FileInfo btd6File = new FileInfo(btd6ExePath);
            Autostart();

            BgThread.AddToQueue(() =>
            {
                while (true)
                {
                    if (BTD_Backend.Natives.Utility.IsProgramRunning(btd6File, out Process proc))
                    {
                        Launch_Button.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            foreach (var item in TempSettings.Instance.LastUsedMods)
                            {
                                if (item.ToLower().EndsWith(".btd6mod"))
                                {
                                    if (Launch_Button.Content != "Inject")
                                        Launch_Button.Content = "Inject";
                                    break;
                                }
                            }
                        }));
                    }
                    else
                    {
                        Launch_Button.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            if (Launch_Button.Content != "Launch")
                                Launch_Button.Content = "Launch";
                        }));
                    }
                    Thread.Sleep(1000);
                }
            });
        }
        private void UserData_UserDataLoaded(object sender, UserData.UserDataEventArgs e)
        {
            /*BTD6_CrashHandler handler = new BTD6_CrashHandler();
            handler.EnableCrashLog();*/
        }

        #region UI Events
        //========================================================

        DispatcherTimer blinkTimer = new DispatcherTimer();
        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            blinkTimer.Tick += Console_Timer_Tick;
            blinkTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);

            BTD6_CrashHandler handler = new BTD6_CrashHandler();
            handler.EnableCrashLog();
        }
        
        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
                overflowGrid.Visibility = Visibility.Collapsed;

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
                mainPanelBorder.Margin = new Thickness();
        }

        bool finishedLoading = false;
        private void Main_Activated(object sender, EventArgs e)
        {
            if (finishedLoading == false)
            {
                finishedLoading = true;
                OnFinishedLoading();
            }
        }

        private void Main_Closing(object sender, CancelEventArgs e) => TempSettings.Instance.SaveSettings();

        private void ConsoleColapsed(object sender, RoutedEventArgs e)
        {
            if (OutputLog.Visibility == Visibility.Collapsed)
            {
                OutputLog.Visibility = Visibility.Visible;
                CollapseConsole_Button.Content = "Hide Console";
            }
            else
            {
                OutputLog.Visibility = Visibility.Collapsed;
                CollapseConsole_Button.Content = "Show Console";
            }
        }
        //startng herere ====================================================
        public void Launch_Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TempSettings.Instance.GetModsDir(SessionData.CurrentGame)))
            {
                Log.Output("Error! You can't launch yet because you need to set a mods directory for your selected game");
                return;
            }

            Launcher.Launch();
        }
        private void NoModsButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.NoModsStart();
        }
        private void Log_MessageLogged(object sender, Log.LogEvents e)
        {
            if (e.Output == OutputType.MsgBox)
                System.Windows.Forms.MessageBox.Show(e.Message);
            else
            {
                OutputLog.Dispatcher.BeginInvoke((Action)(() =>
                {
                    OutputLog.AppendText(e.Message);
                    OutputLog.ScrollToEnd();
                }));
                
                if (e.Output == OutputType.Both)
                    System.Windows.Forms.MessageBox.Show(e.Message.Replace(">> ",""));
            }

            if (TempSettings.Instance.ConsoleFlash && OutputLog.Visibility == Visibility.Collapsed)
                blinkTimer.Start();
        }

        // The timer's Tick event.
        private bool BlinkOn = false;
        private int blinkCount = 0;
        private void Console_Timer_Tick(object sender, EventArgs e)
        {
            var consoleButtonColor = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
            var consoleDarkButtonColor = new SolidColorBrush(Color.FromArgb(255, 62, 62, 62));

            if (blinkCount >= 6)
            {
                BlinkOn = false;
                blinkCount = 0;
                CollapseConsole_Button.Background = consoleButtonColor;
                CollapseConsole_Button.Foreground = Brushes.Black;
                blinkTimer.Stop();
                return;
            }

            if (BlinkOn)
            {
                CollapseConsole_Button.Foreground = Brushes.Black;
                CollapseConsole_Button.Background = consoleButtonColor;
            }
            else
            {
                CollapseConsole_Button.Background = consoleDarkButtonColor;
                CollapseConsole_Button.Foreground = Brushes.White;
            }

            BlinkOn = !BlinkOn;
            blinkCount++;
        }
        #endregion
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        private void Autostart()
        {
            if(TempSettings.Instance.AutoStart==true)
            {
                Launcher.Launch();
            }
            else
            {
                return;
            }
        }
        private void NoModsAutostart()
        {
            if (TempSettings.Instance.NoModsAutoStart == true)
            {
                Launcher.NoModsStart();
            }
            else
            {
                return;
            }
        }
    }
}