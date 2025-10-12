namespace TgaBuilderLib.Abstraction
{
    public interface IView
    {
        Task ShowAsync();

        Task HideAsync();

        Task CloseAsync();

        Task<bool?> ShowDialogAsync();

        bool? DialogResult { get; set; }

        object? DataContext { get; set; }

        bool IsLoaded { get; }
    }
}
