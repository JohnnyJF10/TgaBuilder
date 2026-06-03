using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.ViewModel;

public abstract class ThrottledViewModelBase : ViewModelBase
{

    private const int RECALC_DELAY_MS = 50;

    private readonly object _recalcLock = new();
    private bool _recalcUpdateRunning;
    private bool _recalcUpdatePending;

    protected abstract bool PreProcess();

    protected abstract Task Recalculate();

    protected async Task TriggerRecalculation()
    {
        lock (_recalcLock)
        {
            if (_recalcUpdateRunning)
            {
                _recalcUpdatePending = true;
                return;
            }

            _recalcUpdateRunning = true;
        }

        try
        {
            do
            {
                lock (_recalcLock)
                {
                    _recalcUpdatePending = false;
                }

                if (!PreProcess())
                    return;

                await Task.Delay(RECALC_DELAY_MS);

                await Recalculate();
            }
            while (_recalcUpdatePending);
        }
        finally
        {
            lock (_recalcLock)
            {
                _recalcUpdateRunning = false;
            }
        }
    }

    protected virtual void SetPropertyTriggerRecalculation<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            _ = TriggerRecalculation();
        }
    }

    protected bool SetCallerPropertyReturn<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            return true;
        }

        return false;
    }
}