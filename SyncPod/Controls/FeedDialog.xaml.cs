using Common.Services;
using Common.Utilities;
using Common.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SyncPod.Controls
{
    /// <summary>
    /// Interaction logic for FeedDialog.xaml
    /// </summary>
    public partial class FeedDialog : Window, IDisposable, INotifyPropertyChanged
    {
        private class RemoveFeedCommand : ICommand
        {
            private EpisodeProvider EpisodeProvider { get; }

            public RemoveFeedCommand(EpisodeProvider episodeProvider)
            {
                EpisodeProvider = episodeProvider;
            }

            public event EventHandler CanExecuteChanged;


            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                EpisodeProvider.FeedUrls.Remove(parameter as string);
            }
        }

        private class AddFeedCommand : ICommand
        {
            private EpisodeProvider EpisodeProvider { get; }
            private FeedDialog FeedDialog { get; }

            public AddFeedCommand(EpisodeProvider episodeProvider, FeedDialog feedDialog)
            {
                EpisodeProvider = episodeProvider;
                FeedDialog = feedDialog;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                var url = parameter as string;
                if (url != null && !EpisodeProvider.FeedUrls.Contains(url))
                    EpisodeProvider.FeedUrls.Add(url);
                FeedDialog.FeedUrlToAdd = "";
            }
        }

        private EpisodeProvider EpisodeProvider { get; }

        public FeedDialog(EpisodeProvider episodeProvider)
        {
            InitializeComponent();

            EpisodeProvider = episodeProvider;
            RemoveCommand = new RemoveFeedCommand(EpisodeProvider);
            AddCommand = new AddFeedCommand(EpisodeProvider, this);

            var binding = new FeedDisplayBinding<string, FeedUrlViewModel>(
                EpisodeProvider.FeedUrls, Feeds,
                url => new FeedUrlViewModel(url, RemoveCommand));

            EpisodeProvider.FeedError += EpisodeProvider_FeedError;

            DataContext = this;
        }

        public void Dispose()
        {
            EpisodeProvider.FeedError -= EpisodeProvider_FeedError;
        }

        public ICommand RemoveCommand { get; }
        public ICommand AddCommand { get; }

        private string _FeedUrlToAdd;

        public event PropertyChangedEventHandler PropertyChanged;

        public string FeedUrlToAdd
        {
            get => _FeedUrlToAdd;
            set
            {
                _FeedUrlToAdd = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FeedUrlToAdd)));
            }
        }

        public ObservableCollection<FeedUrlViewModel> Feeds { get; }
             = new ObservableCollection<FeedUrlViewModel>();

        private void EpisodeProvider_FeedError(object sender, FeedErrorEventArgs e)
        {
            var feed = Feeds.FirstOrDefault(f => f.Url == e.FeedUrl);
            if (feed != null)
            {
                feed.HasError = true;
            }
        }
    }
}
