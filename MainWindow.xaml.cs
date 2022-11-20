using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SharpCompress.Archives.Rar;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Controls;
using SharpCompress.Archives;
using MySql.Data.MySqlClient;
using System.Windows.Markup;
using System.Windows.Input;
using SharpCompress.Common;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System.IO;
using System;
using Data;

namespace Polynurm_Launcher
{
    public partial class MainWindow : Window
    {
        private string polynurmFolder;
        private bool downloading = false;
        private Border[] listInformation;

        private List<string[]> databaseInformation = new List<string[]>();
        private List<BitmapImage> cachedPreviews = new List<BitmapImage>();


        public MainWindow()
        {
            InitializeComponent();
            ConnectDatabase();
            SetDefaultDirectory();
            InitializeScrollView();
            InitializeTitleBarEventHandler();
            RefreshItemList();
        }

        private void InitializeTitleBarEventHandler()
        {
            SizeChanged += new SizeChangedEventHandler(UpdateMinimizeRestoreButton);
        }

        private void SetDefaultDirectory()
        {
            if (Properties.Settings.Default.InstallDirectory == "null")
            {
                polynurmFolder = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), "Polynurm Studios");
            }
            else
            {
                polynurmFolder = Properties.Settings.Default.InstallDirectory;
            }
        }

        //Database is as follows
        //0: name
        //1: description
        //2: preview
        //3: link
        //4: fileName
        //5: fileSize
        public void RefreshItemList()
        {
            for (int i = 0; i < databaseInformation.Count; i++)
            {
                string[] row = databaseInformation[i];
                Border Item = listInformation[i];
                Label ItemVersion = (Label)Item.FindName("ItemTemplateVersion");
                Label ItemDirectory = (Label)Item.FindName("ItemTemplateDirectory");
                Button ItemButtonPrimary = (Button)Item.FindName("ItemTemplatePrimaryButton");
                Button ItemButtonSecondary = (Button)Item.FindName("ItemTemplateSecondaryButton");
                ItemDirectory.Content = Path.Combine(polynurmFolder, row[0]);
                if (!downloading)
                {
                    if (ExecutablePath(row[0]) != null)
                    {
                        ItemButtonPrimary.Content = "Play";
                        ItemButtonSecondary.Content = "Reinstall";
                        ItemButtonSecondary.Visibility = Visibility.Visible;
                        ItemVersion.Content = $"Version {GetVersion(row[0])}";
                        if (UpdateAvailable(row[0], row[4]))
                        {
                            ItemButtonSecondary.Content = "Update";
                            ItemVersion.Content = $"Version {GetVersion(row[0])} => {GetVersionValue(row[4])}";
                        }
                    }
                    else
                    {
                        ItemButtonPrimary.Content = "Install";
                        ItemButtonSecondary.Visibility = Visibility.Collapsed;
                        ItemVersion.Content = $"Size {row[5]}";
                    }
                }
            }
        }

        public void UpdateButtonDownloadMode(Button button)
        {
            button.Content = "Please Wait...";
        }

        public void InitializeScrollView()
        {
            listInformation = new Border[databaseInformation.Count];
            ItemTemplateParent.Visibility = Visibility.Collapsed;
            for (int i = 0; i < databaseInformation.Count; i++)
            {
                string[] row = databaseInformation[i];
                Border Item = XamlReader.Parse(XamlWriter.Save(ItemTemplateParent)) as Border;
                Label ItemLabel = (Label)Item.FindName("ItemTemplateLabel");
                Label ItemVersion = (Label)Item.FindName("ItemTemplateVersion");
                Label ItemDirectory = (Label)Item.FindName("ItemTemplateDirectory");
                Button ItemButtonPrimary = (Button)Item.FindName("ItemTemplatePrimaryButton");
                Button ItemButtonSecondary = (Button)Item.FindName("ItemTemplateSecondaryButton");
                Button ItemButtonExpander = (Button)Item.FindName("ItemTemplateExpanderButton");
                Image ItemButtonExpanderImage = (Image)Item.FindName("ItemTemplateExpanderImage");
                TextBlock ItemButtonExpanderText = (TextBlock)Item.FindName("ItemTemplateExpanderText");
                Border ItemTemplateExpanderParent = (Border)Item.FindName("ItemTemplateExpanderParent");

                ItemLabel.Content = row[0];
                ItemVersion.Content = "Version 1.0";
                ItemDirectory.Content = polynurmFolder;

                ItemButtonPrimary.Click += (object sender, RoutedEventArgs e) =>
                {
                    if (downloading) { return; }
                    if (ExecutablePath(row[0]) != null)
                    {
                        Play(row[0]);
                    }
                    else
                    {
                        DownloadGame(row[4], row[3]);
                        UpdateButtonDownloadMode(ItemButtonPrimary);
                    }
                };

                ItemButtonSecondary.Click += (object sender, RoutedEventArgs e) =>
                {
                    if (downloading) { return; }
                    DownloadGame(row[4], row[3]);
                    UpdateButtonDownloadMode(ItemButtonSecondary);
                };

                ItemButtonExpander.Click += (object sender, RoutedEventArgs e) =>
                {
                    DoubleAnimation transitionHeight = new DoubleAnimation { Duration = TimeSpan.FromSeconds(1) };
                    DoubleAnimation transitionFade = new DoubleAnimation { Duration = TimeSpan.FromSeconds(1) };
                    CubicEase ease = new CubicEase(); ease.EasingMode = EasingMode.EaseOut;
                    transitionHeight.EasingFunction = ease; transitionFade.EasingFunction = ease;
                    if (Item.MaxHeight < 500)
                    {
                        transitionHeight.From = 80; transitionHeight.To = 370;
                        transitionFade.From = 0; transitionFade.To = 1;
                        DescriptionTransition(ItemButtonExpanderText, row[1]);
                        Item.MaxHeight = 500;
                    }
                    else
                    {
                        transitionHeight.From = 370; transitionHeight.To = 80;
                        transitionFade.From = 1; transitionFade.To = 0;
                        Item.MaxHeight = 400;
                    }
                    Item.BeginAnimation(HeightProperty, transitionHeight);
                    ItemTemplateExpanderParent.BeginAnimation(OpacityProperty, transitionFade);
                };

                ItemButtonExpanderImage.Source = cachedPreviews[i];
                ItemButtonExpanderText.Text = row[1];
                Item.Visibility = Visibility.Visible;
                ItemContainer.Children.Add(Item);
                listInformation[i] = Item;
            }
        }

        private void ConnectDatabase()
        {
            DBConnection dbCon = DBConnection.Instance();
            dbCon.Server = "polynurm.fi";
            dbCon.DatabaseName = "agileape_polynurm_database";
            dbCon.UserName = "agileape_app";
            dbCon.Password = "gu24DkEWqZ8y7PJ";
            if (dbCon.IsConnect())
            {
                string query = $"SELECT * FROM app";
                MySqlCommand cmd = new MySqlCommand(query, dbCon.Connection);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string[] row = new string[6];
                    for (int i = 0; i < 6; i++)
                    {
                        row[i] = reader.GetString(i);
                    }
                    cachedPreviews.Add(new BitmapImage(new Uri(row[2], UriKind.Absolute)));
                    databaseInformation.Add(row);
                }
                reader.Close();
                dbCon.Close();
            }
        }

        private void DescriptionTransition(TextBlock target, string destination)
        {
            string current = string.Empty;
            Random rnd = new Random();
            int len = destination.Length;
            int[] indexes = new int[len];
            for (int i = 0; i < len; i++) { indexes[i] = i; }
            indexes = indexes.OrderBy(x => rnd.Next()).ToArray();
            char[] chr = "Tyulq".ToCharArray();
            for (int i = 0; i < current.Length; i++)
            {
                if (i >= destination.Length) break;
                if (current[i] == ' ' && destination[i] != ' ')
                {
                    current = current.Remove(i, 1).Insert(i, chr[rnd.Next(0, chr.Length)].ToString());
                }
                else if (current[i] != ' ' && destination[i] == ' ')
                {
                    current = current.Remove(i, 1).Insert(i, " ");
                }
            }
            if (current.Length < destination.Length || true)
            {
                current = current.PadRight(destination.Length).Substring(0, destination.Length);
            }
            Storyboard story = new Storyboard();
            story.FillBehavior = FillBehavior.HoldEnd;
            story.RepeatBehavior = new RepeatBehavior(1);
            DiscreteStringKeyFrame discreteStringKeyFrame;
            StringAnimationUsingKeyFrames stringAnimationUsingKeyFrames = new StringAnimationUsingKeyFrames();
            stringAnimationUsingKeyFrames.Duration = new Duration(TimeSpan.FromMilliseconds(600));
            for (int i = 0; i < indexes.Length; i++)
            {
                char c = ' ';
                if (indexes[i] < destination.Length)
                {
                    c = destination[indexes[i]];
                }
                current = current.Remove(indexes[i], 1).Insert(indexes[i], c.ToString());
                discreteStringKeyFrame = new DiscreteStringKeyFrame();
                discreteStringKeyFrame.KeyTime = KeyTime.Paced;
                discreteStringKeyFrame.Value = current;
                stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
            }
            Storyboard.SetTargetName(stringAnimationUsingKeyFrames, target.Name);
            Storyboard.SetTargetProperty(stringAnimationUsingKeyFrames, new PropertyPath(TextBlock.TextProperty));
            story.Children.Add(stringAnimationUsingKeyFrames);
            story.Begin(target);
        }

        private string ExecutablePath(string name)
        {
            string directory = Path.Combine(polynurmFolder, name, name + ".exe");
            if (File.Exists(directory))
            {
                return directory;
            }
            else
            {
                return null;
            }
        }

        private void Play(string name)
        {
            Process.Start(ExecutablePath(name));
        }

        private void OpenDirectory(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(polynurmFolder))
            {
                Process.Start(polynurmFolder);
            }
            else
            {
                MessageBox.Show("Directory has not been created yet.", "Directory not found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SelectDirectory(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = openFileDlg.ShowDialog();
            string directoryResult = Path.Combine(openFileDlg.SelectedPath, "Polynurm Studios");
            if (openFileDlg.SelectedPath.Length < 2) { return; }
            polynurmFolder = directoryResult;
            Properties.Settings.Default.InstallDirectory = directoryResult;
            Properties.Settings.Default.Save();
            RefreshItemList();
        }
        private void CheckPolynurmFolder()
        {
            if (!Directory.Exists(polynurmFolder))
            {
                Directory.CreateDirectory(polynurmFolder);
            }
        }

        private void DownloadGame(string rarName, string driveLink)
        {
            downloading = true;
            RefreshItemList();
            CheckPolynurmFolder();
            string DownloadPath = Path.Combine(polynurmFolder, rarName);
            GoogleDriveDownloadHandler downloadHandler = new GoogleDriveDownloadHandler();
            downloadHandler.BeginDownload(driveLink, DownloadPath);
            downloadHandler.DownloadFileCompleted += (sender, e) => Extract(rarName, DownloadPath);
        }

        private void Extract(string rarName, string rarPath)
        {
            string extractPath = polynurmFolder;
            bool Interrupted = false;
            try
            {
                using (RarArchive archive = RarArchive.Open(rarPath))
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
                Interrupted = true;
            }
            SafeDelete(rarPath);
            downloading = false;
            if (!Interrupted) { SaveVersionInformation(rarName); }
            RefreshItemList();
        }

        private bool UpdateAvailable(string name, string rarName)
        {
            string currentVersion = GetVersion(name);
            string latestVersion = GetVersionValue(rarName);
            return !currentVersion.Contains(latestVersion);
        }

        private string GetVersionControl()
        {
            return Properties.Settings.Default.VersionControl == "null" ? "" : Properties.Settings.Default.VersionControl;
        }

        private string GetVersionValue(string rarName)
        {
            string[] fileNameSplit = rarName.Split(' ');
            string end = fileNameSplit[fileNameSplit.Length - 1];
            string version = end.Substring(0, end.Length - 4);
            return version;
        }

        private string GetNameFromRarName(string rarName)
        {
            string[] splits = rarName.Split(' ');
            int len = splits[splits.Length - 1].Length;
            return rarName.Substring(0, rarName.Length - len);
        }

        private string GetVersion(string name)
        {
            string[] versionControl = GetVersionControl().Split('\n');
            for (int i = 0; i < versionControl.Length; i++)
            {
                if (versionControl[i].Contains(name))
                {
                    return $"{versionControl[i + 1]}";
                }
            }
            return "";
        }

        private void SaveVersionInformation(string rarName)
        {
            string name = GetNameFromRarName(rarName);
            string version = GetVersionValue(rarName);
            string versionControl = GetVersionControl();
            string[] versionControlArray = versionControl.Split('\n');
            int versionControlIndex = -1;
            for (int i = 0; i < versionControlArray.Length; i++)
            {
                if (versionControlArray[i].Contains(name))
                {
                    versionControlIndex = i;
                    break;
                }
            }
            if (versionControlIndex == -1)
            {
                versionControl += $"{name}\n{version}\n";
            }
            else
            {
                versionControlArray[versionControlIndex + 1] = version;
                versionControl = "";
                foreach (string split in versionControlArray)
                {
                    versionControl += $"{split}\n";
                }
            }
            Properties.Settings.Default.VersionControl = versionControl;
            Console.WriteLine(versionControl);
            Properties.Settings.Default.Save();
        }

        private void SafeDelete(string url)
        {
            bool containsCorrectName = false;
            foreach (string[] row in databaseInformation)
            {
                if (url.Contains(row[0]))
                {
                    containsCorrectName = true;
                    break;
                }
            }
            if (File.Exists(url))
            {
                if (Path.GetExtension(url) == ".rar")
                {
                    if (containsCorrectName)
                    {
                        File.Delete(url);
                    }
                }
            }
        }

        private void SetToolTipPosition(object sender, MouseEventArgs e)
        {
            ChooseDirectoryToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            ChooseDirectoryToolTip.HorizontalOffset = e.GetPosition((IInputElement)sender).X + 10;
            ChooseDirectoryToolTip.VerticalOffset = e.GetPosition((IInputElement)sender).Y + 10;

            OpenDirectoryToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            OpenDirectoryToolTip.HorizontalOffset = e.GetPosition((IInputElement)sender).X + 10;
            OpenDirectoryToolTip.VerticalOffset = e.GetPosition((IInputElement)sender).Y + 10;
        }

        /*Custom Title Bar Functionality*/
        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.WindowState = WindowState.Minimized;
        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            //change the WindowStyle back to None, but only after the Window has been activated
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => WindowStyle = WindowStyle.None));
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RefreshMaximizeRestoreButton()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.maximizeButton.Visibility = Visibility.Hidden;
                this.restoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                this.maximizeButton.Visibility = Visibility.Visible;
                this.restoreButton.Visibility = Visibility.Hidden;
            }
        }

        private void UpdateMinimizeRestoreButton(object sender, EventArgs e)
        {
            this.RefreshMaximizeRestoreButton();
        }
    }
}
