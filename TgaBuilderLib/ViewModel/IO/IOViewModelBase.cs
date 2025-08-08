using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Enums;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.Utils;

namespace TgaBuilderLib.ViewModel
{
    public abstract class IOViewModelBase : ViewModelBase
    {
        public IOViewModelBase(
            Func<ViewIndex, IView> getViewCallback,

            IFileService fileService,
            IMessageService messageService,
            IImageFileManager imageManager,
            ILogger logger,
            IUsageData usageData,

            TexturePanelViewModelBase panel)
        {
            _getViewCallback = getViewCallback;
            _fileService = fileService;
            _messageService = messageService;
            _imageManager = imageManager;
            _logger = logger;
            _usageData = usageData;
            _panel = panel;
        }

        protected readonly Func<ViewIndex, IView> _getViewCallback;
        protected readonly IFileService _fileService;
        protected readonly IMessageService _messageService;
        protected readonly IImageFileManager _imageManager;
        protected readonly ILogger _logger;
        protected readonly IUsageData _usageData;

        protected TexturePanelViewModelBase _panel;

        protected CancellationTokenSource? _cancellationTokenSource;
        protected Task? _ioTask;

        protected string _lastFilePath = string.Empty;

        private bool _isLoading;
        private bool _controlsEnabled = true;
        private bool _isDropHintVisible = true;


        public string LastFileName => Path.GetFileName(_lastFilePath);

        public bool IsLoading
        {
            get => _isLoading;
            set => SetPropertyPrimitive(ref _isLoading, value, nameof(IsLoading));
        }

        public bool ControlsEnabled
        {
            get => _controlsEnabled;
            set => SetPropertyPrimitive(ref _controlsEnabled, value, nameof(ControlsEnabled));
        }

        public bool IsDropHintVisible
        {
            get => _isDropHintVisible;
            set => SetPropertyPrimitive(ref _isDropHintVisible, value, nameof(IsDropHintVisible));
        }

        public void CancelOpen()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            SetControlsStateAfterLoading();
        }

        protected void SetControlsStateForLoading()
        {
            IsLoading = true;
            ControlsEnabled = false;
        }

        protected void SetControlsStateAfterLoading()
        {
            IsLoading = false;
            ControlsEnabled = true;
            IsDropHintVisible = false;
        }

        protected static bool IsHandleableOpenFileException(Exception e) =>
            e is FileNotFoundException
            or DirectoryNotFoundException
            or FileFormatException
            or NotSupportedException
            or InvalidOperationException;
    }
}
