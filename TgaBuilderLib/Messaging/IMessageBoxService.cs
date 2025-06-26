using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgaBuilderLib.Abstraction;

namespace TgaBuilderLib.Messaging
{
    public enum YesNoCancel
    {
        Yes,
        No,
        Cancel
    }

    public interface IMessageBoxService
    {
        public Task<bool> ShowOkCancelMessageBox(string Header, string Message);
        public Task<YesNoCancel> ShowYesNoCancelMessageBox(string Header, string Message);
        public Task ShowErrorMessageBox(string Header, string Message, Exception? ex = null);
        public Task ShowInfoMessageBox(string Header, string Message);
    }
}
