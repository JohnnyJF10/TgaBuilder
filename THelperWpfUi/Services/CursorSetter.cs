using System.Windows.Input;
using THelperLib.Abstraction;

namespace THelperWpfUi.Services
{
    public class CursorSetter : ICursorSetter
    {
        public CursorSetter(
            Cursor pipetteCursor
            ) 
        {
            _pipetteCursor = pipetteCursor;
        }

        private Cursor _pipetteCursor; 

        public void SetEyedropperCursor()
            => Mouse.OverrideCursor = _pipetteCursor;

        public void SetDefaultCursor()
            => Mouse.OverrideCursor = null;
    }
}
