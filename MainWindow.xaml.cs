﻿using Data;
using MySql.Data.MySqlClient;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Polynurm_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string polynurmFolder;
        private string currentRarName = null;
        private string currentGameLink = null;
        private string currentGameFolder = null;
        private bool downloading = false;
        private int currentTab = 0;

        #pragma warning disable IDE0044 // Add readonly modifier
        private List<string[]> databaseInformation = new List<string[]>();
        #pragma warning restore IDE0044 // Add readonly modifier

        #pragma warning disable IDE0044 // Add readonly modifier
        private List<BitmapImage> cachedPreviews = new List<BitmapImage>();
        #pragma warning restore IDE0044 // Add readonly modifier

        private PolyBackground polyBackground;


        public MainWindow()
        {
            //Startup stuff
            InitializeComponent();
            ConnectDatabase();
            Home_Click(null, null);
            UpdateConfig();

            //User Set Directory
            if(Properties.Settings.Default.InstallDirectory == "null")
            {
                //Default Directory
                polynurmFolder = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), "Polynurm Studios");
            }
            else
            {
                polynurmFolder = Properties.Settings.Default.InstallDirectory;
            }
            dirLabel.Content = polynurmFolder;

            //Set background
            polyBackground = new PolyBackground();
            polyBackground.CreatePolyCanvas();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += polyBackground.PolyCanvasTick;
            timer.Start();
        }

        private string GetGameVersion (string fileName)
        {
            string[] fileNameSplit = fileName.Split(' ');
            string end = fileNameSplit[fileNameSplit.Length - 1];
            string version = end.Substring(0, end.Length - 4);
            return version;
        }

        private void DragWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
        private string UpdateConfig()
        {
            Install_Button.Visibility = Visibility.Visible;
            Repair_Button.Visibility = Visibility.Hidden;
            if(currentTab == 0)
            {
                Install_Button.Visibility = Visibility.Hidden;
                return null;
            }
            else if(currentTab == 1) {currentGameFolder = Path.Combine(polynurmFolder, "CloneQube", "CloneQube.exe");}
            else if(currentTab == 2) { currentGameFolder = Path.Combine(polynurmFolder, "The Ether Orb", "The Ether Orb.exe");}
            else if (currentTab == 3) { currentGameFolder = Path.Combine(polynurmFolder, "JokerPokerSimulator", "JokerPokerSim.exe");}
            if (File.Exists(currentGameFolder))
            {
                Install_Button.Content = "Play";
                Repair_Button.Visibility = Visibility.Visible;
                return currentGameFolder;
            }
            else
            {
                Install_Button.Content = "Install";
                return null;
            }
        }

        private void TabFadeIn()
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation
            {
                From = 20,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
            };
            QuadraticEase quadEase = new QuadraticEase();
            quadEase.EasingMode = EasingMode.EaseOut;
            doubleAnimation.EasingFunction = quadEase;
            
            animatedTab.RenderTransform.BeginAnimation(TranslateTransform.YProperty, doubleAnimation);
            doubleAnimation.From = 15;
            doubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(800));
            mainPost.RenderTransform.BeginAnimation(TranslateTransform.YProperty, doubleAnimation);
            doubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(1500));
            sidePost.RenderTransform.BeginAnimation(TranslateTransform.YProperty, doubleAnimation);
            doubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            upcoming.RenderTransform.BeginAnimation(TranslateTransform.YProperty, doubleAnimation);
            disclaimer.RenderTransform.BeginAnimation(TranslateTransform.YProperty, doubleAnimation);

            doubleAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(1000))
            };
            animatedTab.BeginAnimation(OpacityProperty, doubleAnimation);
            doubleAnimation.From = 0.5;
            mainPost.BeginAnimation(OpacityProperty, doubleAnimation);
            sidePost.BeginAnimation(OpacityProperty, doubleAnimation);
            upcoming.BeginAnimation(OpacityProperty, doubleAnimation);
            disclaimer.BeginAnimation(OpacityProperty, doubleAnimation);

        }

        private void ConnectDatabase()
        {
            DBConnection dbCon = DBConnection.Instance();
            //If you are reading this, this account has readonly privileges, so you can't delete my tables (I think...)
            dbCon.Server = "polynurm.fi";
            dbCon.DatabaseName = "agileape_polynurm_database";
            dbCon.UserName = "agileape_app";
            dbCon.Password = "gu24DkEWqZ8y7PJ";
            if (dbCon.IsConnect())
            {
                string[] games = new string[4] {"latestUpdate","CloneQube","The Ether Orb","Joker Poker Simulator"};
                for(int gameI = 0; gameI < games.Length; gameI++)
                {
                    string[] row = new string[6];
                    string query = $"SELECT * FROM app WHERE gameName='{games[gameI]}'";
                    MySqlCommand cmd = new MySqlCommand(query, dbCon.Connection);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        for(int i = 0; i < 6; i++) {
                            row[i] = reader.GetString(i);
                        }
                    }
                    if(row[2] != "null")
                    {
                        cachedPreviews.Add(new BitmapImage(new Uri(row[2], UriKind.Absolute)));
                    }
                    databaseInformation.Add(row);
                    reader.Close();
                }
                dbCon.Close();
            }
        }

        private void UpdateTabInfo()
        {
            if(currentTab == 0) { return; }
            Game_Grid.Visibility = Visibility.Visible;
            Home_Grid.Visibility = Visibility.Hidden;
            string[] tabInfo = databaseInformation[currentTab];
            Title.Content = tabInfo[0];
            DescriptionTransition(tabInfo[1]);

            var brush = new ImageBrush();
            brush.ImageSource = cachedPreviews[currentTab];
            Preview.Background = brush;

            currentGameLink = tabInfo[3];
            currentRarName = tabInfo[4];

            string currentVersion = GetVersionInformation();
            string latestVersion = GetGameVersion(currentRarName);

            if(File.Exists(currentGameFolder) && currentVersion == latestVersion)
            {
                Info_Label.Content = "Latest Version Installed: " + currentVersion;
            }
            else if (File.Exists(currentGameFolder))
            {
                Info_Label.Content = "New Update Available: " + currentVersion + " => " + latestVersion;
            }
            else
            {
                Info_Label.Content = "Required Space: " + tabInfo[5];
            }  
        }

        private void DescriptionTransition(string destination)
        {
            string origin = descriptionLabel.Text;
            string current = descriptionLabel.Text;
            Random rnd = new Random();
            int len = (destination.Length > current.Length || true) ? destination.Length : current.Length;
            int[] indexes = new int[len];
            for(int i = 0; i < len; i++) {indexes[i] = i;}
            indexes = indexes.OrderBy(x => rnd.Next()).ToArray();
            char[] chr = "Tyulq".ToCharArray();
            if(current == destination || true)
            {
                current = string.Empty;
            }
            for(int i = 0; i < current.Length; i++)
            {
                if (i >= destination.Length) break;
                if(current[i] == ' ' && destination[i] != ' ')
                {
                    current = current.Remove(i, 1).Insert(i, chr[rnd.Next(0,chr.Length)].ToString());
                }
                else if(current[i] != ' ' && destination[i] == ' ')
                {
                    current = current.Remove(i, 1).Insert(i, " ");
                }
            }
            if(current.Length < destination.Length || true)
            {
                current = current.PadRight(destination.Length).Substring(0, destination.Length);
            }
            Storyboard story = new Storyboard();
            story.FillBehavior = FillBehavior.HoldEnd;
            story.RepeatBehavior = new RepeatBehavior(1);
            DiscreteStringKeyFrame discreteStringKeyFrame;
            StringAnimationUsingKeyFrames stringAnimationUsingKeyFrames = new StringAnimationUsingKeyFrames();
            stringAnimationUsingKeyFrames.Duration = new Duration(TimeSpan.FromMilliseconds(400));
            for(int i = 0; i < indexes.Length; i++)
            {
                char c = ' ';
                if(indexes[i] < destination.Length)
                {
                    c = destination[indexes[i]];
                }
                current = current.Remove(indexes[i], 1).Insert(indexes[i], c.ToString());
                discreteStringKeyFrame = new DiscreteStringKeyFrame();
                discreteStringKeyFrame.KeyTime = KeyTime.Paced;
                discreteStringKeyFrame.Value = current;
                stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
            }
            Storyboard.SetTargetName(stringAnimationUsingKeyFrames, descriptionLabel.Name);
            Storyboard.SetTargetProperty(stringAnimationUsingKeyFrames, new PropertyPath(TextBlock.TextProperty));
            story.Children.Add(stringAnimationUsingKeyFrames);
            story.Begin(descriptionLabel);
        }

        private void Tab_Activity()
        {
            TabFadeIn();
            UpdateConfig();
            UpdateTabInfo();
        }

        private void CloneQube_Tab_Button_Click(object sender, RoutedEventArgs e)
        {
            if(currentTab == 1 || downloading) { return; } currentTab = 1; Tab_Activity();
        }
        private void The_Ether_Orb_Tab_Button_Click(object sender, RoutedEventArgs e)
        {
            if (currentTab == 2 || downloading) { return; } currentTab = 2; Tab_Activity();
        }
        private void JokerPokerSim_Tab_Button_Click(object sender, RoutedEventArgs e)
        {
            if (currentTab == 3 || downloading) { return; } currentTab = 3; Tab_Activity();
        }
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            if (downloading) { return; }
            if (currentTab != 0) { TabFadeIn(); }
            currentTab = 0;
            Game_Grid.Visibility = Visibility.Hidden;
            Home_Grid.Visibility = Visibility.Visible;
            UpdateConfig();
            string[] tabInfo = databaseInformation[currentTab];
            Title.Content = "";
            Preview.Background = Brushes.Black;
            mainPostText.Text = tabInfo[1];
            upcomingText.Text = tabInfo[3];
            mainPostImage.Source = cachedPreviews[currentTab];
            Info_Label.Content = "";
        }

        private void HyperlinkRequest(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Install_Button_Click(object sender, RoutedEventArgs e)
        {
            string path = UpdateConfig();
            if(path != null) {
                Process.Start(path);
            }
            else
            {
                DownloadGame(currentRarName, currentGameLink);
            }
        }
        private void Repair_Button_Click(object sender, RoutedEventArgs e)
        {
            DownloadGame(currentRarName, currentGameLink);
        }

        private void SelectDirectory(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = openFileDlg.ShowDialog();
            string dir = Path.Combine(openFileDlg.SelectedPath, "Polynurm Studios");
            if (openFileDlg.SelectedPath.Length < 2) {return;}
            dirLabel.Content = dir;
            polynurmFolder = dir;
            Properties.Settings.Default.InstallDirectory = dir;
            Properties.Settings.Default.Save();
            UpdateConfig();
        }
        private void CheckPolynurmFolder()
        {
            if (!Directory.Exists(polynurmFolder)) { Directory.CreateDirectory(polynurmFolder); }
        }

        private void DownloadGame(string rarName, string driveLink)
        {
            if(downloading) { return; }
            downloading = true;
            loadingBalls.Visibility = Visibility.Visible;
            Info_Label.Content = "Downloading...";
            CheckPolynurmFolder();
            string DownloadPath = Path.Combine(polynurmFolder, rarName);
            FileDownloader fileDownloader = new FileDownloader();
            fileDownloader.DownloadFileCompleted += (sender, e) => Extract(DownloadPath);
            fileDownloader.DownloadFileAsync(driveLink, DownloadPath);
        }

        private void Extract(string downloadURL)
        {
            Info_Label.Content = "Extracting...";
            string extractPath = polynurmFolder;
            string callBack = "Download Successful";
            bool failed = false;
            try
            {
                using (RarArchive archive = RarArchive.Open(downloadURL))
                {
                    foreach (RarArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(extractPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
            catch (Exception)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                callBack = "Download Failed";
                failed = true;
            }
            Info_Label.Content = callBack;
            SafeDelete(downloadURL);
            loadingBalls.Visibility = Visibility.Hidden;
            downloading = false;
            if (!failed) { SaveVersionInformation(); }
            UpdateConfig();
        }

        //TODO: Fix this mess.
        private string GetVersionInformation()
        {
            if (currentRarName.Contains("CloneQube"))
            {
                return Properties.Settings.Default.CloneQubeVersion;
            }
            else if (currentRarName.Contains("The Ether Orb"))
            {
                return Properties.Settings.Default.TheEtherOrbVersion;
            }
            else if (currentRarName.Contains("JokerPokerSimulator"))
            {
                return Properties.Settings.Default.JokerPokerSimulatorVersion;
            }
            return string.Empty;
        }

        private void SaveVersionInformation()
        {
            if(currentRarName.Contains("CloneQube"))
            {
                Properties.Settings.Default.CloneQubeVersion = GetGameVersion(currentRarName);
                Properties.Settings.Default.Save();
            }
            else if(currentRarName.Contains("The Ether Orb"))
            {
                Properties.Settings.Default.TheEtherOrbVersion = GetGameVersion(currentRarName);
                Properties.Settings.Default.Save();
            }
            else if (currentRarName.Contains("JokerPokerSimulator"))
            {
                Properties.Settings.Default.JokerPokerSimulatorVersion = GetGameVersion(currentRarName);
                Properties.Settings.Default.Save();
            }
        }

        private void SafeDelete(string url)
        {
            bool check1 = url.Contains("CloneQube");
            bool check2 = url.Contains("The Ether Orb");
            bool check3 = url.Contains("JokerPokerSimulator");
            if (File.Exists(url))
            {
                if(Path.GetExtension(url) == ".rar")
                {
                    if(check1 || check2 || check3)
                    {
                        File.Delete(url);
                    }
                }
            }
        }
        private void Exit_Button_Click(object sender, RoutedEventArgs e) { Environment.Exit(0);}
        private void Minimize_Button_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
    }
}