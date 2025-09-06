namespace TgaBuilderLib.Commands
{
    public interface ICommandManagerService
    {
        /// <summary>
        /// forces a reevaluation of all CanExecute properties on commands.
        /// </summary>
        void InvalidateRequerySuggested();

        /// <summary>
        /// Will be raised when the command manager asks whether commands can execute.
        /// </summary>
        event EventHandler RequerySuggested;
    }
}
