using Common.Models;
using Common.Services;
using Common.Utilities;
using Microsoft.Win32;
using SyncPod.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
using System.Windows.Forms;
using SyncPod.Controls;
using Common.ViewModels;

namespace SyncPod
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private EpisodeProvider EpisodeProvider { get; }
        private SettingsManager SettingsManager { get; } = new SettingsManager();
        private DownloadManager DownloadManager { get; } = new DownloadManager();

        public MainWindow()
        {
            InitializeComponent();
            EpisodeProvider = new EpisodeProvider(new HttpClient());
            EpisodeProvider.LoadProgress += EpisodeProvider_LoadProgress;
            var binding = new FeedDisplayBinding<EpisodeModel, EpisodeViewModel>(
                EpisodeProvider.Episodes,
                Episodes,
                ep => DownloadManager.AddEpisode(ep),
                ep => DownloadManager.RemoveEpisode(ep));
            binding.MaxItems = 400;
            DataContext = this;
            DownloadManager.PropertyChanged += DownloadManager_PropertyChanged;

            RunTask(Initialize());
        }

        private void EpisodeProvider_LoadProgress(object sender, (int, int) e)
        {
            if (e.Item2 == 0)
            {
                FeedLoadPregressBar.Visibility = Visibility.Collapsed;
                NotLoading = true;
            }
            else
            {
                NotLoading = false;
                FeedLoadPregressBar.Visibility = Visibility.Visible;
                FeedLoadPregressBar.Maximum = e.Item2;
                FeedLoadPregressBar.Value = e.Item1;
            }
        }

        private async Task Initialize()
        {
            NotLoading = false;
            await SettingsManager.LoadSettings();
            Folder = SettingsManager.GetValue("folder", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            var feeds = SettingsManager.GetValue("feeds", new string[0]);
            foreach (var feed in feeds)
            {
                EpisodeProvider.FeedUrls.Add(feed);
            }
            NotLoading = true;
        }

        private void RunTask(Func<Task> task) => RunTask(task());
        private async void RunTask(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, ex.Message);
            }
        }

        public ObservableCollection<EpisodeViewModel> Episodes { get; }
            = new ObservableCollection<EpisodeViewModel>();

        private bool _NotLoading = true;
        public bool NotLoading
        {
            get => _NotLoading;
            set
            {
                _NotLoading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotLoading)));
            }
        }

        private string _Folder;
        public string Folder
        {
            get => _Folder;
            set
            {
                _Folder = value;
                DownloadManager.FolderPath = Folder;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Folder)));
            }
        }

        private bool _FolderError;
        public bool FolderError
        {
            get => _FolderError;
            private set
            {
                _FolderError = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FolderError)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog()
            {
                SelectedPath = Folder,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Folder = dialog.SelectedPath;
                SettingsManager.SetValue("folder", Folder);
                RunTask(SettingsManager.SaveSettings());
            }
        }

        private void ManageFeedsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FeedDialog(EpisodeProvider);
            dialog.ShowDialog();
            SettingsManager.SetValue("feeds", EpisodeProvider.FeedUrls.ToArray());
            RunTask(SettingsManager.SaveSettings());
        }

        private void DownloadManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DownloadManager.FolderError))
            {
                FolderError = DownloadManager.FolderError;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadManager.RefreshDownloads();
        }
    }
}
