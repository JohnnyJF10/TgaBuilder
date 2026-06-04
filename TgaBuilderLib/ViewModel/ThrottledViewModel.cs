using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.ViewModel;

public abstract class ThrottledViewModelBase : ViewModelBase
{
    protected abstract Task TriggerRecalculation();

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