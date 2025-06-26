namespace TgaBuilderLib.Messaging
{    
    public interface IMessageService
    {
        void SendMessage(
            MessageType message,
            string additionalInfo = "",
            Exception? ex = null);
    }
}
