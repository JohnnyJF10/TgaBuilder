using TgaBuilderLib.Enums;

namespace TgaBuilderLib.Commands
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
