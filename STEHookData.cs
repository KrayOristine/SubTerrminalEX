using System.Runtime.InteropServices;

namespace SubTerminalEX {

    /// <summary>
    /// Data about hook method
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct STEHookData {
        /// <summary>
        /// Action that run when target hooked command is triggered
        /// </summary>
        public Action<List<string>> Func {get; set; }
        /// <summary>
        /// Should this hook run at which point of time
        /// </summary>
        public STEHookTarget Target {get; internal set; }
        /// <summary>
        /// Priority of this hook
        /// </summary>
        public STEHookPriority Priority { get; internal set; }
        /// <summary>
        /// Priority of this hook in number
        /// </summary>
        public int PriorityInt { get; internal set; }

        /// <summary>
        /// Target command in string
        /// </summary>
        public string TargetCommand { get; internal set; }

        internal STEHookData(Action<List<string>> func, string command, STEHookTarget target, STEHookPriority prio) {
            Func = func;
            Target = target;
            TargetCommand = command;
            Priority = prio;
            PriorityInt = (int)prio;
        }

        internal STEHookData(Action<List<string>> func, string command, STEHookTarget target, int prio) {
            Func = func;
            Target = target;
            TargetCommand = command;
            Priority = STEHookPriority.None;
            PriorityInt = prio;
        }
    }
}