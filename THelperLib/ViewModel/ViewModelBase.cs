using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected void OnCallerPropertyChanged([CallerMemberName]string? propertyName = null)
            => OnPropertyChanged(propertyName ?? "");

        protected void SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected void SetCallerProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName ?? "");
            }
        }

        protected void SetPropertyPrimitive(ref int field, int value, string propertyName)
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected void SetPropertyPrimitive(ref bool field, bool value, string propertyName)
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }
    }
}
