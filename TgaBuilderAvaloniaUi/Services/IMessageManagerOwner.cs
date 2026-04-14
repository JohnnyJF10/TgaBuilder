using Avalonia.Notification;

namespace TgaBuilderAvaloniaUi.Services
{
    internal interface IMessageManagerOwner
    {
        public INotificationMessageManager Manager { get; }
    }
}
