using TgaBuilderAvaloniaUi.Elements;

namespace TgaBuilderAvaloniaUi.View;

public enum MessageBoxResult
{
    Ok,
    Cancel,
    Yes,
    No
}

public partial class MessageBoxWindow : AsyncWindow
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

    public string Header { get; }
    public string Message { get; }

    public MessageBoxWindow(string header, string message, string type)
    {
        InitializeComponent();
        Header = header;
        Message = message;
        DataContext = this;

        // Alle Buttons erstmal verstecken
        OkButton.IsVisible = false;
        CancelButton.IsVisible = false;
        YesButton.IsVisible = false;
        NoButton.IsVisible = false;

        // Abhängig vom Typ Buttons anzeigen
        switch (type)
        {
            case "Error":
            case "Info":
                OkButton.IsVisible = true;
                break;
            case "OkCancel":
                OkButton.IsVisible = true;
                CancelButton.IsVisible = true;
                break;
            case "YesNoCancel":
                YesButton.IsVisible = true;
                NoButton.IsVisible = true;
                CancelButton.IsVisible = true;
                break;
        }

        // Events
        OkButton.Click += (_, _) => CloseDialog(MessageBoxResult.Ok);
        CancelButton.Click += (_, _) => CloseDialog(MessageBoxResult.Cancel);
        YesButton.Click += (_, _) => CloseDialog(MessageBoxResult.Yes);
        NoButton.Click += (_, _) => CloseDialog(MessageBoxResult.No);
    }

    private void CloseDialog(MessageBoxResult result)
    {
        Result = result;
        Close();
    }

    public MessageBoxWindow()
    {
        InitializeComponent();
        Header = "Header";
        Message = "This is a test message";
        DataContext = this;
    }
}