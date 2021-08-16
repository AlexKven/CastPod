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

namespace SyncPod
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private EpisodeProvider EpisodeProvider { get; }
        private SettingsManager SettingsManager { get; } = new SettingsManager();

        public MainWindow()
        {
            InitializeComponent();
            EpisodeProvider = new EpisodeProvider(new HttpClient());
            var binding = new FeedDisplayBinding<EpisodeModel, EpisodeModel>(
                EpisodeProvider.Episodes,
                Episodes,
                ep => ep);
            binding.MaxItems = 2000;
            DataContext = this;
            //EpisodeProvider.FeedUrls.Add("http://feeds.feedburner.com/netRocksFullMp3Downloads");
            //EpisodeProvider.FeedUrls.Add("https://feeds.megaphone.fm/WWO8086402096");
            //EpisodeProvider.FeedUrls.Add("http://www.espn.com/espnradio/podcast/feeds/itunes/podCast?id=14554755");
            //EpisodeProvider.FeedUrls.Add("https://feeds.megaphone.fm/theweeds");
            //EpisodeProvider.FeedUrls.Add("https://feeds.megaphone.fm/VMP5705694065");

            RunTask(Initialize());
        }

        private async Task Initialize()
        {
            NotLoading = false;
            await SettingsManager.LoadSettings();
            Folder = SettingsManager.GetValue("folder", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            var feeds = SettingsManager.GetValue("feeds", new List<string>()
            {
                "http://feeds.feedburner.com/netRocksFullMp3Downloads",
                "https://feeds.megaphone.fm/WWO8086402096"
            });
            foreach (var feed in feeds)
            {
                EpisodeProvider.FeedUrls.Add(feed);
            }
            await SettingsManager.SaveSettings();
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

        public ObservableCollection<EpisodeModel> Episodes { get; }
            = new ObservableCollection<EpisodeModel>();

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Folder)));
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
            }
        }

        private void ManageFeedsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FeedDialog(EpisodeProvider);
            dialog.ShowDialog();
        }
    }
}
