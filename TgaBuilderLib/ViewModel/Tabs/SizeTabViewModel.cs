using System.ComponentModel;
using System.Runtime.CompilerServices;
using TgaBuilderLib.Messaging;

namespace TgaBuilderLib.ViewModel
{
    public class SizeTabViewModel : ViewModelBase
    {
        public SizeTabViewModel(
            IMessageService messageService,

            TargetTexturePanelViewModel destination)
        {
            _messageService = messageService;

            _destination = destination;
            _numPagesX = _destination.Presenter.PixelWidth / PAGE_SIZE;
            _numPagesY = _destination.Presenter.PixelHeight / PAGE_SIZE;

            SubscribeToDestinationChanges();
        }


        private const int PAGE_SIZE = 256;
        private const int MAX_NUM_PAGES = 128;

        private readonly IMessageService _messageService;
        private readonly TargetTexturePanelViewModel _destination;

        private bool _sortedResizing = true;
        private int _numPagesX;
        private int _numPagesY;


        public bool SortedResizing
        {
            get => _sortedResizing;
            set => SetCallerProperty(ref _sortedResizing, value);
        }

        public int NumPagesX
        {
            get => _numPagesX;
            set => SetNumPagesX(value);
        }

        public int NumPagesY
        {
            get => _numPagesY;
            set => SetNumPagesY(value);
        }



        private void SetNumPagesX(int value)
        {
            var newValue = CalculateNewPageXValue(value, _numPagesX);

            if (newValue == _numPagesX) 
                return;

            SetNewPageNumAndResize(ref _numPagesX, newValue, nameof(NumPagesX));
        }

        private void SetNumPagesY(int value)
        {
            var newValue = Math.Clamp(value, 1, MAX_NUM_PAGES);

            if (newValue == _numPagesY) 
                return;

            SetNewPageNumAndResize(ref _numPagesY, newValue, nameof(NumPagesY));
        }

        private void SubscribeToDestinationChanges()
        {
            _destination.PropertyChanged += OnDestinationPropertyChanged;
        }

        private void OnDestinationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TargetTexturePanelViewModel.Presenter))
            {
                UpdatePageNum();
            }
        }

        public void UpdatePageNum()
        {
            if (_destination.Presenter == null) return;

            _numPagesX = _destination.Presenter.PixelWidth / PAGE_SIZE;
            _numPagesY = _destination.Presenter.PixelHeight / PAGE_SIZE;

            NotifyPagesChanged();
        }

        private void SetNewPageNumAndResize(ref int field, int value, [CallerMemberName] string? propertyName = null)
        {
            if (field == value || string.IsNullOrEmpty(propertyName)) return;

            bool isSortedResizing = _sortedResizing && propertyName == nameof(NumPagesX);

            if (isSortedResizing )
            {
                float widthRatio = value / (float)_numPagesX;
                if ((int)(_numPagesY / widthRatio) > MAX_NUM_PAGES)
                {
                    _messageService.SendMessage(
                        MessageType.SortedRezisingNoPossible);
                    return;
                }
            }

            field = Math.Max(1, value);

            ResizeDestination(isSortedResizing);

            OnPropertyChanged(propertyName);
        }

        private void ResizeDestination(bool isSortedResizing)
        {
            int newWidth = _numPagesX * PAGE_SIZE;
            int newHeight = _numPagesY * PAGE_SIZE;

            if (isSortedResizing)
                _destination.ResizePresenterSorted(newWidth);
            else
                _destination.ResizePresenter(newWidth, newHeight);
        }

        private void NotifyPagesChanged()
        {
            OnPropertyChanged(nameof(NumPagesX));
            OnPropertyChanged(nameof(NumPagesY));
        }

        private int CalculateNewPageXValue(int proposedValue, int currentValue)
            => proposedValue < currentValue ? proposedValue switch
            {
                < 2 => 1,
                < 4 => 2,
                < 8 => 4,
                _ => 8
            }
            : proposedValue switch
            {
                > 8 => 16,
                > 4 => 8,
                > 2 => 4,
                _ => 2
            };
    }
}