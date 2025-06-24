using System.Windows.Media.Imaging;

namespace THelperLib.UndoRedo
{
    public interface IUndoableAction
    {
        public long SizeInBytes { get; }
        public void Undo();
        public void Redo();
        public void ReturnData();
    }
}
