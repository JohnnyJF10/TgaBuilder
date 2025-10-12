using Avalonia.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderAvaloniaUi.Services
{
    internal interface IMessageManagerOwner
    {
        public INotificationMessageManager Manager { get; }
    }
}
