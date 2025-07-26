using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.Messaging;

namespace TgaBuilderLib.ViewModel
{
    public class FormatTabViewModel : ViewModelBase
    {
        public FormatTabViewModel(
            IMessageBoxService messageBoxService,
            TargetTexturePanelViewModel target)
        {
            _messageBoxService = messageBoxService;
            _target = target;

            _target.PresenterChanged += (_, _) => OnPropertyChanged(nameof(IsBgra32));
        }

        private readonly IMessageBoxService _messageBoxService;
        private readonly TargetTexturePanelViewModel _target;

        public bool IsBgra32
        {
            get => _target.Presenter.Format == PixelFormats.Bgra32;
            set => SetIsBgra32(value);
        }

        private void SetIsBgra32(bool value)
        {
            if ((_target.Presenter.Format == PixelFormats.Bgra32) == value)
                return;

            if (value)
            {
                _target.ConvertToBgra32();
            }
            else
            {
                var result = _messageBoxService.ShowOkCancelMessageBox(
                    "Convert to Rgb24",
                    "Converting to Rgb24 will discard alpha channel information. " +
                    "Are you sure you want to proceed?")
                    .Result;

                if (result == true)
                    _target.ConvertToRgb24();
            }
            OnPropertyChanged(nameof(IsBgra32));
        }
    }
}
