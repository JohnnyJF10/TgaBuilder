using Microsoft.Extensions.DependencyInjection;
using Pfim;
using System.Buffers;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapBytesIO;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Level;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.UndoRedo;
using TgaBuilderLib.Utils;
using TgaBuilderLib.ViewModel;
using TgaBuilderLib.ViewModel.Elements;
using TgaBuilderLib.ViewModel.Views;
using TgaBuilderWpfUi.Services;
using TgaBuilderWpfUi.View;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;

namespace TgaBuilderWpfUi
{
    public partial class App
    {
        private const int PAGE_SIZE = 256;
        private const int PANEL_WIDTH_INIT = 256;
        private const int PANEL_HEIGHT_INIT = 1536;
        private const int APP_DEFAULT_DPI = 96;
        private const int SELECTION_SIZE_INIT = 64;
        private const int BYTES_PER_PIXEL_4 = 4;

        enum PresenterType
        {
            Source = 0,
            Target = 1,
            Selection = 2,
        }

        private void BuildServicesDI(IServiceCollection services)
        {
            AddUIServicesToProvider(services);
            AddCoreServicesToProvider(services);
            AddBitmapFactoryProvider(services);
            AddFactoriesToProvider(services);
            AddElementVMsToProvider(services);
            AddControlVMsToProvider(services);
            AddPanelVMsToProvider(services);
            AddTabVMsToProvider(services);
            AddViewVMsToProvider(services);
            AddViewsToProvider(services);
        }

        private void AddFactoriesToProvider(IServiceCollection services)
        {
            services.AddSingleton<Func<string, int, bool, LevelBase>>(sp =>
                (fileName, trTexturePanelHorPagesNum, useTrTextureRepacking) =>
                    new TrLevel(fileName, trTexturePanelHorPagesNum, useTrTextureRepacking,
                        sp.GetRequiredService<ITrngDecrypter>()));

            services.AddSingleton<Func<string, int, LevelBase>>(sp =>
                (fileName, trTexturePanelHorPagesNum) =>
                    new TenLevel(fileName, trTexturePanelHorPagesNum));
        }

        private void AddCoreServicesToProvider(IServiceCollection services)
        {
            services.AddSingleton<ITrngDecrypter, TrngDecrypter>();
            services.AddSingleton<IBitmapBytesIO, BitmapBytesIO>();

            services.AddSingleton<IImageFileManager, ImageFileManager>(sp => new ImageFileManager(
                trLevelFactory: sp.GetRequiredService<Func<string, int, bool, LevelBase>>(),
                tenLevelFactory: sp.GetRequiredService<Func<string, int, LevelBase>>(),
                bitmapIO: sp.GetRequiredService<IBitmapBytesIO>()));

            services.AddSingleton<IAsyncFileLoader, AsyncFileLoader>();
            services.AddSingleton<IBitmapOperations, BitmapOperations>();
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IEyeDropper, EyeDropper>();

            services.AddSingleton<IUsageData, UsageData>(_ => UsageData.Load());
            services.AddSingleton<IUndoRedoManager, UndoRedoManager>(sp => new UndoRedoManager(
                maxMemoryBytes: sp.GetRequiredService<IUsageData>().UndoRedoMemoryBytes
            ));
        }

        private void AddUIServicesToProvider(IServiceCollection services)
        {
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ICursorSetter, CursorSetter>(sp => new CursorSetter(
                    new(Application.GetResourceStream(
                        new Uri("Resources/eyedropper.cur", UriKind.Relative)).Stream)));
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<IMessageBoxService, MessageBoxService>();
        }

        private void AddBitmapFactoryProvider(IServiceCollection services)
        {
            services.AddSingleton<Func<int, int, bool, WriteableBitmap>>(sp =>
                (width, height, hasAlpha) => new WriteableBitmap(
                    pixelWidth: width,
                    pixelHeight: height,
                    dpiX: APP_DEFAULT_DPI,
                    dpiY: APP_DEFAULT_DPI,
                    pixelFormat: hasAlpha ? PixelFormats.Bgra32 : PixelFormats.Rgb24,
                    palette: null));
        }

        private void AddElementVMsToProvider(IServiceCollection services)
        {
            services.AddSingleton(typeof(Color), Color.FromArgb(255, 255, 0, 255));

            services.AddSingleton(sp => new EyeDropper(
                color: sp.GetRequiredService<Color>()
            ));

            services.AddSingleton(sp => new SingleSelectionShapeViewModel(
                initSize: SELECTION_SIZE_INIT));

            services.AddSingleton(sp => new SingleSelectionShapeViewModel(
                initSize: SELECTION_SIZE_INIT));

            services.AddSingleton<PanelVisualSizeViewModel>(); //_panel
            services.AddSingleton<PanelVisualSizeViewModel>(); //SourceTexturePanelViewModel

            services.AddTransient(sp => new AnimSelectShapeViewModel(
                panelWidth: PANEL_WIDTH_INIT,
                stepSize: SELECTION_SIZE_INIT));

            services.AddTransient(sp => new SelectionShapeViewModel(
                maxX: PANEL_WIDTH_INIT,
                maxY: PANEL_HEIGHT_INIT));

            services.AddTransient(sp => new PickerViewModel(
                initSize: SELECTION_SIZE_INIT,
                initMaxSize: PANEL_WIDTH_INIT));

            services.AddSingleton(sp => new VisualGridViewModel(
                cellSize: SELECTION_SIZE_INIT));
        }

        private void AddControlVMsToProvider(IServiceCollection services)
        {
            services.AddSingleton<AnimationViewModel>();

            services.AddSingleton(sp => new SelectionViewModel(
                logger: sp.GetRequiredService<ILogger>(),
                messageService: sp.GetRequiredService<IMessageService>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>(),
                presenter: GetBitmapFromFactory(sp, SELECTION_SIZE_INIT, SELECTION_SIZE_INIT, true)));

            services.AddSingleton(sp => new SourceIOViewModel(
                getViewCallback: idx => sp.GetServices<IView>().ElementAt((int)idx),
                fileService: sp.GetRequiredService<IFileService>(),
                messageService: sp.GetRequiredService<IMessageService>(),
                imageManager: sp.GetRequiredService<IImageFileManager>(),
                logger: sp.GetRequiredService<ILogger>(),
                usageData: sp.GetRequiredService<IUsageData>(),
                panel: sp.GetRequiredService<SourceTexturePanelViewModel>()));

            services.AddSingleton(sp => new TargetIOViewModel(
                getViewCallback: idx => sp.GetServices<IView>().ElementAt((int)idx),
                fileService: sp.GetRequiredService<IFileService>(),
                messageService: sp.GetRequiredService<IMessageService>(),
                messageBoxService: sp.GetRequiredService<IMessageBoxService>(),
                undoRedoManager: sp.GetRequiredService<IUndoRedoManager>(),
                imageManager: sp.GetRequiredService<IImageFileManager>(),
                logger: sp.GetRequiredService<ILogger>(),
                usageData: sp.GetRequiredService<IUsageData>(),
                panel: sp.GetRequiredService<TargetTexturePanelViewModel>()));
        }

        private void AddPanelVMsToProvider(IServiceCollection services)
        {
            services.AddSingleton(sp => new SourceTexturePanelViewModel(
                cursorSetter: sp.GetRequiredService<ICursorSetter>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>(),
                eyeDropper: sp.GetRequiredService<IEyeDropper>(),

                presenter: GetBitmapFromFactory(sp, PANEL_WIDTH_INIT, PANEL_HEIGHT_INIT, true),

                SelectionVM: sp.GetRequiredService<SelectionViewModel>(),
                AnimationVM: sp.GetRequiredService<AnimationViewModel>(),
                pickerVM: sp.GetRequiredService<PickerViewModel>(),
                animSelectShapeVM: sp.GetRequiredService<AnimSelectShapeViewModel>(),
                selectionShapeVM: sp.GetRequiredService<SelectionShapeViewModel>(),
                visualGridVM: sp.GetRequiredService<VisualGridViewModel>()
            ));

            services.AddSingleton(sp => new TargetTexturePanelViewModel(
                cursorSetter: sp.GetRequiredService<ICursorSetter>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>(),
                eyeDropper: sp.GetRequiredService<IEyeDropper>(),
                undoRedoManager: sp.GetRequiredService<IUndoRedoManager>(),

                presenter: GetBitmapFromFactory(sp, PANEL_WIDTH_INIT, PANEL_HEIGHT_INIT, true),

                SelectionVM: sp.GetRequiredService<SelectionViewModel>(),
                AnimationVM: sp.GetRequiredService<AnimationViewModel>(),
                originalPosShapeVM: sp.GetServices<SingleSelectionShapeViewModel>()
                                            .ElementAt((int)PresenterType.Source),
                targetPosShapeVM: sp.GetServices<SingleSelectionShapeViewModel>()
                                            .ElementAt((int)PresenterType.Target),
                pickerVM: sp.GetRequiredService<PickerViewModel>(),
                animSelectShapeVM: sp.GetRequiredService<AnimSelectShapeViewModel>(),
                selectionShapeVM: sp.GetRequiredService<SelectionShapeViewModel>()
            ));
        }

        private void AddTabVMsToProvider(IServiceCollection services)
        {
            services.AddSingleton(sp => new SizeTabViewModel(
                messageService: sp.GetRequiredService<IMessageService>(),
                destination: sp.GetRequiredService<TargetTexturePanelViewModel>()));

            services.AddSingleton(sp => new PlacingTabViewModel(
                destination: sp.GetRequiredService<TargetTexturePanelViewModel>()));

            services.AddSingleton(sp => new EditTabViewModel(
                destination: sp.GetRequiredService<TargetTexturePanelViewModel>()));

            services.AddTransient(sp => new FormatTabViewModel(
                eyeDropper: sp.GetRequiredService<IEyeDropper>(),

                selection: sp.GetRequiredService<SelectionViewModel>(),
                panel: sp.GetRequiredService<SourceTexturePanelViewModel>()));

            services.AddTransient(sp => new FormatTabViewModel(
                eyeDropper: sp.GetRequiredService<IEyeDropper>(),

                selection: sp.GetRequiredService<SelectionViewModel>(),
                panel: sp.GetRequiredService<TargetTexturePanelViewModel>(),
                messageBoxService: sp.GetRequiredService<IMessageBoxService>()));

            services.AddTransient(sp => new ViewTabViewModel(
                visualPanelSize: sp.GetServices<PanelVisualSizeViewModel>()
                                        .ElementAt((int)PresenterType.Source),
                panel: sp.GetRequiredService<SourceTexturePanelViewModel>()));

            services.AddTransient(sp => new ViewTabViewModel(
                visualPanelSize: sp.GetServices<PanelVisualSizeViewModel>()
                                        .ElementAt((int)PresenterType.Target),
                panel: sp.GetRequiredService<TargetTexturePanelViewModel>()));
        }

        private void AddViewVMsToProvider(IServiceCollection services)
        {
            services.AddTransient<AboutViewModel>();

            services.AddTransient(sp => new BatchLoaderViewModel(
                fileService: sp.GetRequiredService<IFileService>(),
                messageService: sp.GetRequiredService<IMessageService>(),

                usageData: sp.GetRequiredService<IUsageData>(),
                asyncFileLoader: sp.GetRequiredService<IAsyncFileLoader>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>(),
                logger: sp.GetRequiredService<ILogger>()));

            services.AddSingleton(sp => new MainViewModel(
                getViewCallback: idx => sp.GetServices<IView>().ElementAt((int)idx),

                messageService: sp.GetRequiredService<IMessageService>(),
                undoRedoManager: sp.GetRequiredService<IUndoRedoManager>(),

                source: sp.GetRequiredService<SourceTexturePanelViewModel>(),
                destination: sp.GetRequiredService<TargetTexturePanelViewModel>(),

                selection: sp.GetRequiredService<SelectionViewModel>(),
                animation: sp.GetRequiredService<AnimationViewModel>(),

                sourceIO: sp.GetRequiredService<SourceIOViewModel>(),
                destinationIO: sp.GetRequiredService<TargetIOViewModel>(),

                placing: sp.GetRequiredService<PlacingTabViewModel>(),
                edits: sp.GetRequiredService<EditTabViewModel>(),
                size: sp.GetRequiredService<SizeTabViewModel>(),

                sourceFormat: sp.GetServices<FormatTabViewModel>()
                                            .ElementAt((int)PresenterType.Source),
                targetFormat: sp.GetServices<FormatTabViewModel>()
                                            .ElementAt((int)PresenterType.Target),


                sourceViewTab: sp.GetServices<ViewTabViewModel>()
                                            .ElementAt((int)PresenterType.Source),
                destinationViewTab: sp.GetServices<ViewTabViewModel>()
                                            .ElementAt((int)PresenterType.Target),

                usageData: sp.GetRequiredService<IUsageData>()));


        }

        private void AddViewsToProvider(IServiceCollection services)
        {
            services.AddSingleton<IView, MainWindow>(sp => new MainWindow(
                    mainViewModel: sp.GetRequiredService<MainViewModel>()));

            services.AddTransient<IView, BatchLoaderWindow>(
                sp => new BatchLoaderWindow(
                    viewModel: sp.GetRequiredService<BatchLoaderViewModel>()));

            services.AddTransient<IView, AboutWindow>(
                sp => new AboutWindow(
                    viewModel: sp.GetRequiredService<AboutViewModel>()));
        }

        private WriteableBitmap GetBitmapFromFactory(IServiceProvider serviceProvider, int width, int height, bool hasAlpha)
            => serviceProvider
                .GetRequiredService<Func<int, int, bool, WriteableBitmap>>()
                .Invoke(width, height, hasAlpha);

    }
}

