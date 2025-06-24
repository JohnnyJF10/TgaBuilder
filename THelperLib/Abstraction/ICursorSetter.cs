using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.Abstraction
{
    public interface ICursorSetter
    {
        public void SetEyedropperCursor();
        public void SetDefaultCursor();
    }
}
