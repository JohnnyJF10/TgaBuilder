﻿namespace TgaBuilderLib.UndoRedo
{
    public class SingleAction : IUndoableAction
    {
        public SingleAction(Action undo, Action redo)
        {
            _undo = undo;
            _redo = redo;
        }

        public long SizeInBytes => 0;

        private readonly Action _undo;
        private readonly Action _redo;

        public void Undo() => _undo();
        public void Redo() => _redo();

        public void ReturnData() { /* No data to return */ }
    }
}
