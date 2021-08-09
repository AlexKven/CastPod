using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Common.Utilities
{
    public interface IViewModelBinding : IDisposable
    {
        bool IsDisposed { get; }
    }

    public abstract class ViewModelBindingBase<TViewModel, TControl> : IViewModelBinding where TViewModel : class where TControl : class
    {
        protected WeakReference<TViewModel> ViewModelReference { get; }
        protected WeakReference<TControl> ControlReference { get; }

        public ViewModelBindingBase(TViewModel viewModel, TControl control)
        {
            ViewModelReference = new WeakReference<TViewModel>(viewModel);
            ControlReference = new WeakReference<TControl>(control);

            SetUp(viewModel, control);
        }

        protected abstract void SetUp(TViewModel viewModel, TControl control);
        protected abstract void TearDown(TViewModel viewModel, TControl control);

        public bool IsDisposed { get; private set; } = false;

        public void Dispose()
        {
            if (IsDisposed)
                return;
            TViewModel viewModel = null;
            TControl control = null;
            ViewModelReference.TryGetTarget(out viewModel);
            ControlReference.TryGetTarget(out control);
            TearDown(viewModel, control);
            IsDisposed = true;
        }
    }

    public abstract class ViewModelCollectionBindingBase<TItem, TControl> : ViewModelBindingBase<ObservableCollection<TItem>, TControl> where TControl : class
    {
        private int _StartIndex = 0;
        public int StartIndex
        {
            get => _StartIndex;
            set => _StartIndex = value;
        }

        protected int Count { get; set; } = 0;

        public ViewModelCollectionBindingBase(ObservableCollection<TItem> viewModel, TControl control) : base(viewModel, control) { }

        protected override void SetUp(ObservableCollection<TItem> viewModel, TControl control)
        {
            viewModel.CollectionChanged += ViewModel_CollectionChanged;
            Reset(viewModel, control);
            Count = viewModel.Count;
        }

        protected void Reset(ObservableCollection<TItem> viewModel, TControl control)
        {
            if (Count > 0)
                RemoveRange(StartIndex, Count, control, viewModel.Skip(StartIndex).Take(Count));
            var index = 0;
            foreach (TItem item in viewModel)
            {
                Insert(StartIndex + index++, item, control);
            }
        }

        private void ViewModel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ObservableCollection<TItem> vm;
            if (IsDisposed)
                return;
            var collection = (ObservableCollection<TItem>)sender;
            TControl control;
            if (!ControlReference.TryGetTarget(out control))
                Dispose();
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                if (ViewModelReference.TryGetTarget(out vm))
                {
                    Reset(vm, control);
                }
            }
            else
            {
                if (e.OldItems != null)
                {
                    foreach (TItem item in e.OldItems)
                    {
                        Remove(item, control);
                    }

                }
                if (e.NewItems != null)
                {
                    var index = e.NewStartingIndex;
                    foreach (TItem item in e.NewItems)
                    {
                        Insert(StartIndex + index++, item, control);
                    }
                }
            }
            if (ViewModelReference.TryGetTarget(out vm))
                Count = vm.Count;
        }

        protected override void TearDown(ObservableCollection<TItem> viewModel, TControl control)
        {
            if (viewModel != null)
                viewModel.CollectionChanged -= ViewModel_CollectionChanged;
            RemoveRange(StartIndex, Count, control, viewModel.Skip(StartIndex).Take(Count));
        }

        protected abstract void Insert(int index, TItem item, TControl control);
        protected abstract void Remove(TItem item, TControl control);
        protected abstract void RemoveRange(int startIndex, int count, TControl control, IEnumerable<TItem> items);
    }
    public class FeedDisplayBinding<TFeed, TDest> : ViewModelBindingBase<ObservableCollection<TFeed>, IList<TDest>>
    {
        private Dictionary<TFeed, int> IndexDictionary { get; }
        private Func<TFeed, TDest> CreateItemFunc { get; }
        private Action<TDest> DestroyItemFunc { get; }

        public FeedDisplayBinding(
            ObservableCollection<TFeed> viewModel,
            IList<TDest> control,
            Func<TFeed, TDest> createItemFunc,
            Action<TDest> destroyItemFunc = null,
            Dictionary<TFeed, int> indexDictionary = null) : base(viewModel, control)
        {
            IndexDictionary = indexDictionary;
            CreateItemFunc = createItemFunc;
            DestroyItemFunc = destroyItemFunc;

            _PreFeedItems.CollectionChanged += _PreFeedItems_CollectionChanged;
            _PostFeedItems.CollectionChanged += _PostFeedItems_CollectionChanged;
        }

        private int PreFeedCount { get; set; } = 0;
        private int PostFeedCount { get; set; } = 0;

        private int _MaxItems = 0;
        public int MaxItems
        {
            get => _MaxItems;
            set
            {
                _MaxItems = value;
                if (ViewModelReference.TryGetTarget(out var viewModel) && ControlReference.TryGetTarget(out var control))
                    UpdateFeed(viewModel, control);
            }
        }

        private Func<TFeed, bool> _FilterFunc;
        public Func<TFeed, bool> FilterFunc
        {
            get => _FilterFunc;
            set
            {
                _FilterFunc = value;
                if (ViewModelReference.TryGetTarget(out var viewModel) && ControlReference.TryGetTarget(out var control))
                    UpdateFeed(viewModel, control);
            }
        }


        private ObservableCollection<TFeed> _PreFeedItems = new ObservableCollection<TFeed>();
        public IList<TFeed> PreFeedItems => _PreFeedItems;


        private ObservableCollection<TFeed> _PostFeedItems = new ObservableCollection<TFeed>();
        public IList<TFeed> PostFeedItems => _PostFeedItems;

        private void UpdateListItem(IList<TDest> list, int index, TFeed item)
        {
            var current = list[index];
            if (current == null)
            {
                if (item != null)
                    list[index] = CreateItemFunc(item);
            }
            else
            {
                if (item == null || !current.Equals(item))
                {
                    DestroyItemFunc?.Invoke(current);
                    list[index] = CreateItemFunc(item);
                }
            }
        }

        protected void UpdatePreFeed(ObservableCollection<TFeed> viewModel, IList<TDest> control)
        {
            int index = 0;
            foreach (var item in PreFeedItems)
            {
                if (index >= PreFeedCount)
                    control.Insert(index, CreateItemFunc(item));
                else
                    UpdateListItem(control, index, item);
                index++;
            }
            for (int i = PreFeedCount - 1; i >= index; i--)
            {
                DestroyItemFunc?.Invoke(control[i]);
                control.RemoveAt(i);
            }
            PreFeedCount = index;
        }

        protected void UpdatePostFeed(ObservableCollection<TFeed> viewModel, IList<TDest> control)
        {
            int index = 0;
            foreach (var item in PostFeedItems)
            {
                if (index >= PostFeedCount)
                    control.Add(CreateItemFunc(item));
                else
                    UpdateListItem(control, control.Count - PostFeedCount + index, item);
                index++;
            }
            for (int i = 0; i < PostFeedCount - index; i++)
            {
                DestroyItemFunc?.Invoke(control[control.Count - 1]);
                control.RemoveAt(control.Count - 1);
            }
            PostFeedCount = index;
        }

        protected void UpdateFeed(ObservableCollection<TFeed> viewModel, IList<TDest> control)
        {
            int start = PreFeedCount;
            int length = control.Count - PreFeedCount - PostFeedCount;

            IEnumerable<TFeed> filtered = viewModel;
            if (FilterFunc != null)
                filtered = filtered.Where(FilterFunc);
            if (MaxItems > 0)
                filtered = filtered.Take(MaxItems);

            int index = 0;
            IndexDictionary?.Clear();
            foreach (var item in filtered)
            {
                if (index >= length)
                    control.Insert(start + index, CreateItemFunc(item));
                else
                    UpdateListItem(control, start + index, item);
                if (IndexDictionary != null)
                    IndexDictionary[item] = index;
                index++;
            }
            for (int i = 0; i < length - index; i++)
            {
                DestroyItemFunc?.Invoke(control[start + index]);
                control.RemoveAt(start + index);
            }
        }

        protected override void SetUp(ObservableCollection<TFeed> viewModel, IList<TDest> control)
        {
            viewModel.CollectionChanged += ViewModel_CollectionChanged;
            UpdateFeed(viewModel, control);
        }

        protected override void TearDown(ObservableCollection<TFeed> viewModel, IList<TDest> control)
        {
            if (viewModel != null)
                viewModel.CollectionChanged -= ViewModel_CollectionChanged;
            control.Clear();
        }

        private void ViewModel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModelReference.TryGetTarget(out var viewModel) && ControlReference.TryGetTarget(out var control))
                UpdateFeed(viewModel, control);
        }

        private void _PreFeedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModelReference.TryGetTarget(out var viewModel) && ControlReference.TryGetTarget(out var control))
                UpdatePreFeed(viewModel, control);
        }

        private void _PostFeedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModelReference.TryGetTarget(out var viewModel) && ControlReference.TryGetTarget(out var control))
                UpdatePostFeed(viewModel, control);
        }
    }
}
