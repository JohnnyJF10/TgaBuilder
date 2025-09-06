using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Abstraction
{
    public interface IDispatcherService
    {
        /// <summary>
        /// Invoke the action on the UI thread.
        /// </summary>
        /// <param name="action"> The action to invoke.</param>
        void Invoke(Action action);

        /// <summary>
        /// Invoke the action on the UI thread asynchronously.
        /// </summary>
        /// <param name="action"> The action to invoke.</param>
        /// <returns> A task that represents the asynchronous operation.</returns>
        Task InvokeAsync(Action action);
    }
}
