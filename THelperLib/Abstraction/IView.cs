using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.Abstraction
{
    public interface IView
    {
        public void Show();

        public void Hide();

        public void Close();

        public bool? ShowDialog();

        public bool? DialogResult { get; set; }

        public object? DataContext { get; set; }

        public bool IsLoaded { get; }
    }
}
