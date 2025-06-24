using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THelperLib.Abstraction;

namespace THelperLib.Commands
{
    public struct MousePanelCommandArgs
    {
        public int x;
        public int y;
        public bool isTarget;
        public MouseAction action;
        public MouseModifier modifier;
    }
}
