using Common.Models;
using Common.Services;
using Common.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace SyncPod
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EpisodeProvider EpisodeProvider { get; }
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
            EpisodeProvider.FeedUrls.Add("http://feeds.feedburner.com/netRocksFullMp3Downloads");
            EpisodeProvider.FeedUrls.Add("https://feeds.megaphone.fm/WWO8086402096");
            EpisodeProvider.FeedUrls.Add("http://www.espn.com/espnradio/podcast/feeds/itunes/podCast?id=14554755");
            EpisodeProvider.FeedUrls.Add("https://feeds.megaphone.fm/theweeds");
            EpisodeProvider.FeedUrls.Add("https://feeds.megaphone.fm/VMP5705694065");
        }

        public ObservableCollection<EpisodeModel> Episodes { get; }
            = new ObservableCollection<EpisodeModel>();
    }
}
