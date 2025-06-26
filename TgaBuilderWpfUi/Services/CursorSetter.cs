using System.Windows.Input;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderWpfUi.Services
{
    public class CursorSetter : ICursorSetter
    {
        public CursorSetter(
            Cursor eyedropperCursor
            ) 
        {
            _eyedropperCursor = eyedropperCursor;
        }

        private Cursor _eyedropperCursor; 

        public void SetEyedropperCursor()
            => Mouse.OverrideCursor = _eyedropperCursor;

        public void SetDefaultCursor()
            => Mouse.OverrideCursor = null;
    }
}
