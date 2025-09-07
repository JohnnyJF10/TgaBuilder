using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Messaging;

using DialogHostAvalonia;

namespace TgaBuilderAvaloniaUi.Services
{
    internal class MessageBoxService : IMessageBoxService
    {
        public Task ShowErrorMessageBox(string Header, string Message, Exception? ex = null)
        {
            throw new NotImplementedException();
        }

        public Task ShowInfoMessageBox(string Header, string Message)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ShowOkCancelMessageBox(string Header, string Message)
        {
            throw new NotImplementedException();
        }

        public Task<YesNoCancel> ShowYesNoCancelMessageBox(string Header, string Message)
        {
            throw new NotImplementedException();
        }
    }
}
