using TrLynxLib.Enums;

namespace TrLynxLib.Commands
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
