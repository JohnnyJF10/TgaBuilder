namespace TgaBuilderLib.UndoRedo
{
    public interface IUndoableAction
    {
        public long SizeInBytes { get; }
        public void Undo();
        public void Redo();
        public void ReturnData();
    }
}
