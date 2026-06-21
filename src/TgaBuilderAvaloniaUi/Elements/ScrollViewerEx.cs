using Avalonia.Controls;
using Avalonia.Input;

namespace TgaBuilderAvaloniaUi.Elements
{
    public class ScrollViewerEx : ScrollViewer
    {
        public double ScrollSpeed { get; set; } = 2000;


        public ScrollViewerEx()
        {
            AddHandler(PointerWheelChangedEvent, OnWheel, handledEventsToo: true);
        }

        private void OnWheel(object? sender, PointerWheelEventArgs e)
        {
            e.Handled = true;

            Offset = Offset.WithY(Offset.Y - e.Delta.Y * ScrollSpeed);
        }
    }
}