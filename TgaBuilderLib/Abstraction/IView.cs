namespace TgaBuilderLib.Abstraction
{
    public interface IView
    {
        public void Show();

        public void Hide();

        public void Close();

        public bool? ShowDialog();

        public bool? DialogResult { get; set; }

        public object? DataContext { get; set; }

        public bool IsLoaded { get; }
    }
}
