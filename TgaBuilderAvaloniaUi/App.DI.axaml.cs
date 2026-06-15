using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using TgaBuilderAvaloniaUi.Services;
using TgaBuilderAvaloniaUi.View;
using TgaBuilderAvaloniaUi.Wrappers;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.BitmapBytesIO;
using TgaBuilderLib.BitmapOperations;
using TgaBuilderLib.FileHandling;
using TgaBuilderLib.Level;
using TgaBuilderLib.Messaging;
using TgaBuilderLib.UndoRedo;
using TgaBuilderLib.Utils;
using TgaBuilderLib.Transitions;
using TgaBuilderLib.Modifications;
using TgaBuilderLib.ViewModel;
using TgaBuilderLib.ViewModel.Elements;
using TgaBuilderLib.ViewModel.Views;
using Avalonia;
using Avalonia.Controls;
using TgaBuilderLib.Krita;
using TgaBuilderLib.Psd;
using FileTypes = TgaBuilderLib.Enums.FileTypes;

namespace TgaBuilderAvaloniaUi
{
    public static class GlobalServiceProvider
    {
        public static IServiceProvider Instance { get; set; } = null!;
    }

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

            GlobalServiceProvider.Instance = services.BuildServiceProvider();
        }

        private void AddFactoriesToProvider(IServiceCollection services)
        {
            services.AddSingleton<Func<string, int, bool, LevelBase>>(sp =>
                (fileName, trTexturePanelHorPagesNum, useTrTextureRepacking) =>
                    new TrLevel(
                        mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                        fileName: fileName,
                        trTexturePanelHorPagesNum: trTexturePanelHorPagesNum,
                        useTrTextureRepacking: useTrTextureRepacking,
                        trngDecrypter: sp.GetRequiredService<ITrngDecrypter>()));

            services.AddSingleton<Func<string, int, LevelBase>>(sp =>
                (fileName, trTexturePanelHorPagesNum) =>
                    new TenLevel(
                        mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                        fileName: fileName,
                        trTexturePanelHorPagesNum: trTexturePanelHorPagesNum));
        }

        private void AddCoreServicesToProvider(IServiceCollection services)
        {
            services.AddSingleton<IKraFileService, KraFileService>();
            services.AddSingleton<IPsdFileService, PsdFileService>();
            services.AddSingleton<ITrngDecrypter, TrngDecrypter>();
            services.AddSingleton<IBitmapBytesIO, BitmapBytesIO>();

            services.AddSingleton<IImageFileManager, ImageFileManager>(sp => new ImageFileManager(
                trLevelFactory: sp.GetRequiredService<Func<string, int, bool, LevelBase>>(),
                tenLevelFactory: sp.GetRequiredService<Func<string, int, LevelBase>>(),
                bitmapIO: sp.GetRequiredService<IBitmapBytesIO>()));

            services.AddSingleton<ITransitionLayerExporter, TransitionLayerExporter>();

            services.AddSingleton<IAsyncFileLoader, AsyncFileLoader>();
            services.AddSingleton<IBitmapOperations, BitmapOperations>();
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IEyeDropper, EyeDropper>(sp => new EyeDropper(
                    new TgaBuilderLib.Abstraction.Color(0, 0, 0, 0)));

            services.AddSingleton<IUsageData, UsageData>(_ => UsageData.Load());
            services.AddSingleton<IUndoRedoManager, UndoRedoManager>(sp => new UndoRedoManager(
                maxMemoryBytes: sp.GetRequiredService<IUsageData>().UndoRedoMemoryBytes
            ));

            services.AddSingleton<ITransitionHelper, TransitionHelper>(sp => new TransitionHelper(
                AccentColor: GetSystemAccentColor(sp)));
            services.AddSingleton<IModificationsHelper, ModificationsHelper>();
        }

        private TgaBuilderLib.Abstraction.Color GetSystemAccentColor(IServiceProvider serviceProvider)
        {
            if (Application.Current?.TryFindResource("SystemAccentColorDark3", out var resource) == true && resource is Avalonia.Media.Color color)
            {
                return new TgaBuilderLib.Abstraction.Color(color.R, color.G, color.B, color.A);
            }
            else
            {
                return new TgaBuilderLib.Abstraction.Color(128, 128, 128, 128); // Fallback to a default accent color (gray)
            }
        }

        private void AddUIServicesToProvider(IServiceCollection services)
        {
            services.AddSingleton<IMediaFactory, MediaFactory>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ICursorSetter, CursorSetter>();
            services.AddSingleton<NotificationManager>();
            services.AddSingleton<IMessageService, MessageService>(sp => new MessageService(
                    manager: sp.GetRequiredService<NotificationManager>(),
                    wetherSendSuccessMessages: sp.GetRequiredService<IUsageData>().WetherSendSuccessMessage));
            services.AddSingleton<IMessageBoxService, MessageBoxService>();
            services.AddSingleton<IDispatcherService, DispatcherService>();
        }

        private void AddBitmapFactoryProvider(IServiceCollection services)
        {
            services.AddSingleton<Func<int, int, bool, WriteableBitmap>>(sp =>
                (width, height, hasAlpha) => new WriteableBitmap(
                    size: new Avalonia.PixelSize(width, height),
                    dpi: new Avalonia.Vector(APP_DEFAULT_DPI, APP_DEFAULT_DPI),
                    format: hasAlpha ? Avalonia.Platform.PixelFormat.Bgra8888 : Avalonia.Platform.PixelFormat.Rgb32,
                    alphaFormat: Avalonia.Platform.AlphaFormat.Unpremul));
        }

        private void AddElementVMsToProvider(IServiceCollection services)
        {
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
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                clipboardService: sp.GetRequiredService<IClipboardService>(),
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
                dispatcherService: sp.GetRequiredService<IDispatcherService>(),
                panel: sp.GetRequiredService<SourceTexturePanelViewModel>()));

            services.AddSingleton(sp => new TargetIOViewModel(
                getViewCallback: idx => sp.GetServices<IView>().ElementAt((int)idx),
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                fileService: sp.GetRequiredService<IFileService>(),
                messageService: sp.GetRequiredService<IMessageService>(),
                messageBoxService: sp.GetRequiredService<IMessageBoxService>(),
                undoRedoManager: sp.GetRequiredService<IUndoRedoManager>(),
                imageManager: sp.GetRequiredService<IImageFileManager>(),
                logger: sp.GetRequiredService<ILogger>(),
                usageData: sp.GetRequiredService<IUsageData>(),
                dispatcherService: sp.GetRequiredService<IDispatcherService>(),
                // Avalonia has no JPEG encoder, so JPG/JPEG are excluded from the output formats.
                writeableImageFormats: FileTypes.TGA | FileTypes.BMP | FileTypes.PNG
                    | FileTypes.KRA | FileTypes.PSD,
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
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
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

            services.AddTransient<IViewTabViewModel>(sp => new ReadOnlyViewTabViewModel(
                visualPanelSize: sp.GetServices<PanelVisualSizeViewModel>()
                                        .ElementAt((int)PresenterType.Source),
                panel: sp.GetRequiredService<SourceTexturePanelViewModel>()));

            services.AddTransient<IViewTabViewModel>(sp => new ReadOnlyViewTabViewModel(
                visualPanelSize: sp.GetServices<PanelVisualSizeViewModel>()
                                        .ElementAt((int)PresenterType.Target),
                panel: sp.GetRequiredService<TargetTexturePanelViewModel>()));
        }

        private void AddViewVMsToProvider(IServiceCollection services)
        {
            services.AddTransient<AboutViewModel>();

            services.AddTransient(sp => new BatchLoaderViewModel(
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                fileService: sp.GetRequiredService<IFileService>(),
                messageService: sp.GetRequiredService<IMessageService>(),

                usageData: sp.GetRequiredService<IUsageData>(),
                asyncFileLoader: sp.GetRequiredService<IAsyncFileLoader>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>(),
                logger: sp.GetRequiredService<ILogger>(),

                presenter: GetBitmapFromFactory(sp, 2 * PANEL_WIDTH_INIT, PANEL_HEIGHT_INIT, true)));


            services.AddSingleton(sp => new ModificationOutViewModel(
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                modificationHelper: sp.GetRequiredService<IModificationsHelper>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>()));

            services.AddSingleton(sp => new ModificationInViewModel(
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                modificationHelper: sp.GetRequiredService<IModificationsHelper>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>(),
                modificationOutViewModel: sp.GetRequiredService<ModificationOutViewModel>()));

            services.AddTransient(sp => new BasicViewModel(
                modificationHelper: sp.GetRequiredService<IModificationsHelper>(),
                modificationInVM: sp.GetRequiredService<ModificationInViewModel>()));

            services.AddTransient(sp => new ColorViewModel(
                modificationHelper: sp.GetRequiredService<IModificationsHelper>(),
                modificationInVM: sp.GetRequiredService<ModificationInViewModel>()));

            services.AddTransient(sp => new ColorOverlayViewModel(
                modificationHelper: sp.GetRequiredService<IModificationsHelper>(),
                modificationInVM: sp.GetRequiredService<ModificationInViewModel>()));

                

            services.AddSingleton(sp => new TransitionOutViewModel(
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>()));

            services.AddSingleton(sp => new TransitionInViewModel(
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>(),
                transitionOutViewModel: sp.GetRequiredService<TransitionOutViewModel>()));

            services.AddTransient(sp => new AnalysisViewModel(
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                transitionInVM: sp.GetRequiredService<TransitionInViewModel>()));

            services.AddTransient(sp => new PivotViewModel(
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                transitionInVM: sp.GetRequiredService<TransitionInViewModel>()));

            services.AddTransient(sp => new EdgeViewModel(
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                transitionInVM: sp.GetRequiredService<TransitionInViewModel>()));

            services.AddTransient(sp => new ShadowViewModel(
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                transitionInVM: sp.GetRequiredService<TransitionInViewModel>()));

            services.AddTransient(sp => new UnderfillingViewModel(
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                transitionInVM: sp.GetRequiredService<TransitionInViewModel>()));

            services.AddTransient(sp => new ExportTransitionViewModel(
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                exporter: sp.GetRequiredService<ITransitionLayerExporter>(),
                fileService: sp.GetRequiredService<IFileService>(),
                messageService: sp.GetRequiredService<IMessageService>()));

            services.AddTransient(sp => new TransitionViewModel(
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                transitionHelper: sp.GetRequiredService<ITransitionHelper>(),
                bitmapOperations: sp.GetRequiredService<IBitmapOperations>(),
                analysisViewModel: sp.GetRequiredService<AnalysisViewModel>(),
                pivotViewModel: sp.GetRequiredService<PivotViewModel>(),
                edgeViewModel: sp.GetRequiredService<EdgeViewModel>(),
                shadowViewModel: sp.GetRequiredService<ShadowViewModel>(),
                underfillingViewModel: sp.GetRequiredService<UnderfillingViewModel>(),
                exportTransitionViewModel: sp.GetRequiredService<ExportTransitionViewModel>(),
                mainViewModel: sp.GetRequiredService<MainViewModel>()));

            services.AddTransient(sp => new ModificationsViewModel(
                mediaFactory: sp.GetRequiredService<IMediaFactory>(),
                modificationsHelper: sp.GetRequiredService<IModificationsHelper>(),
                mainViewModel: sp.GetRequiredService<MainViewModel>(),
                basicVM: sp.GetRequiredService<BasicViewModel>(),
                colorVM: sp.GetRequiredService<ColorViewModel>(),
                colorOverlayVM: sp.GetRequiredService<ColorOverlayViewModel>()));

            services.AddSingleton(sp => new MainViewModel(
                getViewCallback: idx => sp.GetServices<IView>().ElementAt((int)idx),

                messageService: sp.GetRequiredService<IMessageService>(),
                undoRedoManager: sp.GetRequiredService<IUndoRedoManager>(),
                dispatcherService: sp.GetRequiredService<IDispatcherService>(),

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


                sourceViewTab: sp.GetServices<IViewTabViewModel>()
                                            .ElementAt((int)PresenterType.Source),
                destinationViewTab: sp.GetServices<IViewTabViewModel>()
                                            .ElementAt((int)PresenterType.Target),

                usageData: sp.GetRequiredService<IUsageData>()));


        }

        private void AddViewsToProvider(IServiceCollection services)
        {
            services.AddSingleton<IView, MainWindow>(sp => new MainWindow(
                    mainViewModel: sp.GetRequiredService<MainViewModel>(),
                    manager: sp.GetRequiredService<NotificationManager>()));

            services.AddTransient<IView, BatchLoaderWindow>(
                sp => new BatchLoaderWindow(
                    viewModel: sp.GetRequiredService<BatchLoaderViewModel>()));

            services.AddTransient<IView, AboutWindow>(
                sp => new AboutWindow(
                    viewModel: sp.GetRequiredService<AboutViewModel>()));

            services.AddTransient<IView, TransitionWindow>(
                sp => new TransitionWindow(
                    viewModel: sp.GetRequiredService<TransitionViewModel>()));

            services.AddTransient<IView, ModificationsWindow>(
                sp => new ModificationsWindow(
                    viewModel: sp.GetRequiredService<ModificationsViewModel>()));
        }

        private IWriteableBitmap GetBitmapFromFactory(IServiceProvider serviceProvider, int width, int height, bool hasAlpha)
            => new WriteableBitmapWrapper(serviceProvider
                .GetRequiredService<Func<int, int, bool, WriteableBitmap>>()
                .Invoke(width, height, hasAlpha));

    }
}
