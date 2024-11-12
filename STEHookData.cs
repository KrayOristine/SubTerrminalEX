using System.Runtime.InteropServices;

namespace SubTerminalEX {
    [StructLayout(LayoutKind.Sequential)]
    public struct STEHookData(Action<List<string>> func, string command, STEHookTarget target, STEHookPriority prio) {
        public Action<List<string>> Func = func;
        public STEHookTarget Target = target;
        public STEHookPriority Priority = prio;
        public string TargetCommand = command;
    }
}