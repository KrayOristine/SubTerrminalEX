using System.Runtime.InteropServices;

namespace SubTerminalEX {

    /// <summary>
    /// Data about hook method
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct STEHookData {
        public Action<List<string>> Func {get; set; }
        public STEHookTarget Target {get; internal set; }
        public STEHookPriority Priority { get; internal set; }
        public string TargetCommand { get; internal set; }

        internal STEHookData(Action<List<string>> func, string command, STEHookTarget target, STEHookPriority prio) {
            Func = func;
            Target = target;
            Priority = prio;
            TargetCommand = command;
        }
    }
}