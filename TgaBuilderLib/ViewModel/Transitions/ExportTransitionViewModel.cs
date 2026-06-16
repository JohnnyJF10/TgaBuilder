using System.Threading.Tasks;
using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;
using TgaBuilderLib.Enums;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.Transitions;

namespace TgaBuilderLib.ViewModel;

/// <summary>
/// Backs the "Export" expander in the Transition Helper window. Lets the user write the
/// current transition (smooth or bricks) to a multi-layer PSD or KRA file.
/// </summary>
public class ExportTransitionViewModel : ViewModelBase
{
    public ExportTransitionViewModel(
        ITransitionHelper transitionHelper,
        ITransitionLayerExporter exporter,
        IFileService fileService,
        IMessageService messageService)
    {
        _transitionHelper = transitionHelper;
        _exporter = exporter;
        _fileService = fileService;
        _messageService = messageService;
    }

    private readonly ITransitionHelper _transitionHelper;
    private readonly ITransitionLayerExporter _exporter;
    private readonly IFileService _fileService;
    private readonly IMessageService _messageService;

    private AsyncCommand? _exportToPsdCommand;
    private AsyncCommand? _exportToKritaCommand;

    // Both buttons open the same Save dialog with PSD and KRA selectable; the button only
    // chooses which format is preselected. The chosen file extension decides what is written.
    public ICommand ExportToPsdCommand => _exportToPsdCommand
        ??= new AsyncCommand(() => Export(FileTypes.PSD));

    public ICommand ExportToKritaCommand => _exportToKritaCommand
        ??= new AsyncCommand(() => Export(FileTypes.KRA));

    private async Task Export(FileTypes defaultType)
    {
        if (!_transitionHelper.IsActive)
        {
            _messageService.SendMessage(MessageType.DestinationSaveError);
            return;
        }

        var dialogResult = await _fileService.SaveFileDialog(
            FileTypes.PSD | FileTypes.KRA,
            title: "Export transition layers",
            defaultType: defaultType);

        if (dialogResult != true)
            return;

        string filePath = _fileService.SelectedPath;

        try
        {
            var layers = _transitionHelper.GetExportLayers();
            await Task.Run(() => _exporter.Export(filePath, layers));
        }
        catch (Exception ex)
        {
            _messageService.SendMessage(MessageType.DestinationSaveError, ex: ex);
            return;
        }

        _messageService.SendMessage(MessageType.DestinationSaveSuccess);
    }
}
