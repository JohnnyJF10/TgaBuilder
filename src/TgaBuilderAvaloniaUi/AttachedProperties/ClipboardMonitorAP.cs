using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TgaBuilderAvaloniaUi.AttachedProperties;

internal class ClipboardMonitorAP : AvaloniaObject
{
    public static readonly AttachedProperty<bool> MonitorClipboardProperty =
        AvaloniaProperty.RegisterAttached<ClipboardMonitorAP, TopLevel, bool>(
            "MonitorClipboard", false);

    public static readonly AttachedProperty<ICommand?> ClipboardChangedCommandProperty =
        AvaloniaProperty.RegisterAttached<ClipboardMonitorAP, TopLevel, ICommand?>(
            "ClipboardChangedCommand");

    private static readonly ConditionalWeakTable<TopLevel, DispatcherTimer> _timers = new();
    private static readonly ConditionalWeakTable<TopLevel, ClipboardState> _states = new();
    private static readonly Random _random = new();
    private const int SampleCount = 5; // Easily adjust how many points you want to check

    private class ClipboardState
    {
        public bool HadImageLastTick { get; set; }
        public PixelSize? LastPixelSize { get; set; }
        public PixelPoint[]? ActiveSamplePoints { get; set; } // Stores the randomized positions chosen for this image size
        public int[]? LastSampledPixels { get; set; } // Stores the sampled pixel color values
    }

    static ClipboardMonitorAP()
    {
        MonitorClipboardProperty.Changed.AddClassHandler<TopLevel>(OnMonitorClipboardChanged);
    }

    public static void SetMonitorClipboard(AvaloniaObject element, bool value) => element.SetValue(MonitorClipboardProperty, value);
    public static bool GetMonitorClipboard(AvaloniaObject element) => element.GetValue(MonitorClipboardProperty);

    public static void SetClipboardChangedCommand(AvaloniaObject element, ICommand? value) => element.SetValue(ClipboardChangedCommandProperty, value);
    public static ICommand? GetClipboardChangedCommand(AvaloniaObject element) => element.GetValue(ClipboardChangedCommandProperty);

    private static void OnMonitorClipboardChanged(TopLevel topLevel, AvaloniaPropertyChangedEventArgs e)
    {
        bool isEnabled = (bool)e.NewValue!;
        if (isEnabled)
            EnableClipboardMonitoring(topLevel);
        else
            DisableClipboardMonitoring(topLevel);
    }

    private static void EnableClipboardMonitoring(TopLevel topLevel)
    {
        if (_timers.TryGetValue(topLevel, out _)) return;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _states.Add(topLevel, new ClipboardState());

        timer.Tick += async (s, e) =>
        {
            var clipboard = topLevel.Clipboard;
            if (clipboard == null) return;

            try
            {
                if (!_states.TryGetValue(topLevel, out var state)) return;

                using var bitmap = await clipboard.TryGetBitmapAsync();

                if (bitmap == null)
                {
                    state.HadImageLastTick = false;
                    state.LastPixelSize = null;
                    state.ActiveSamplePoints = null;
                    state.LastSampledPixels = null;
                    return;
                }

                bool isNewImage = false;

                // Step 1: Perform a rough check using the dimensions
                if (!state.HadImageLastTick || state.LastPixelSize != bitmap.PixelSize)
                {
                    isNewImage = true;
                    // Generate new random points tailored to these new image dimensions
                    state.ActiveSamplePoints = GenerateRandomSamplePoints(bitmap.PixelSize, SampleCount);
                }
                else
                {
                    // Step 2: If the size is identical, sample the previously generated coordinates.
                    if (state.ActiveSamplePoints != null)
                    {
                        var currentSamples = GetPixelSamples(bitmap, state.ActiveSamplePoints);

                        if (state.LastSampledPixels == null || !currentSamples.SequenceEqual(state.LastSampledPixels))
                        {
                            isNewImage = true;
                            state.LastSampledPixels = currentSamples;
                        }
                    }
                }

                // If the heuristic detects a change -> trigger the command
                if (isNewImage)
                {
                    state.HadImageLastTick = true;
                    state.LastPixelSize = bitmap.PixelSize;

                    // If it was triggered by step 1, initialize the samples for the next tick
                    if (state.LastSampledPixels == null && state.ActiveSamplePoints != null)
                    {
                        state.LastSampledPixels = GetPixelSamples(bitmap, state.ActiveSamplePoints);
                    }

                    var command = GetClipboardChangedCommand(topLevel);
                    if (command?.CanExecute(null) == true)
                    {
                        command.Execute(null);
                    }
                }
            }
            catch
            {
                // Handle OS-level clipboard access locks
            }
        };

        _timers.Add(topLevel, timer);
        timer.Start();
    }

    private static void DisableClipboardMonitoring(TopLevel topLevel)
    {
        if (_timers.TryGetValue(topLevel, out var timer))
        {
            timer.Stop();
            _timers.Remove(topLevel);
            _states.Remove(topLevel);
        }
    }

    private static PixelPoint[] GenerateRandomSamplePoints(PixelSize size, int count)
    {
        if (size.Width <= 2 || size.Height <= 2) return Array.Empty<PixelPoint>();

        var points = new PixelPoint[count];
        for (int i = 0; i < count; i++)
        {
            // Pick random locations, avoiding the absolute 1-pixel outer edge boundary
            int x = _random.Next(1, size.Width - 1);
            int y = _random.Next(1, size.Height - 1);
            points[i] = new PixelPoint(x, y);
        }
        return points;
    }

    private static int[] GetPixelSamples(Bitmap bitmap, PixelPoint[] points)
    {
        if (points == null || points.Length == 0) return Array.Empty<int>();

        var samples = new int[points.Length];

        // Pin the target array directly in memory
        unsafe
        {
            fixed (int* pSamples = samples)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    var rect = new PixelRect(points[i], new PixelSize(1, 1));

                    // Calculate the pointer directly to the exact index location in the array
                    int* pCurrentPixel = pSamples + i;

                    // Since int* can be cast directly to IntPtr (nint):
                    bitmap.CopyPixels(rect, (IntPtr)pCurrentPixel, sizeof(int), 4);
                }
            }
        }

        return samples;
    }
}