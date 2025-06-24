using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.Messaging
{
    public interface IMessageService
    {
        void SendMessage(
            MessageType message,
            string additionalInfo = "",
            Exception? ex = null);
    }
}
