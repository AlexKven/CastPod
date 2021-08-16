using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Common.ViewModels
{
    public class FeedUrlViewModel : ObservableObject
    {
        public string Url { get; private set; }
        public ICommand RemoveCommand { get; private set; }

        public FeedUrlViewModel(string url, ICommand removeCommand)
        {
            Url = url;
            RemoveCommand = removeCommand;
        }

        private bool _HasError = false;
        public bool HasError
        {
            get => _HasError;
            set => SetProperty(ref _HasError, value);
        }
    }
}
