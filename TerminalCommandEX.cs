

namespace SubTerminalEX {
    public class TerminalCommandEX : ScriptableObject
    {
        public bool CheckArgs(List<string> args) {
            args.RemoveAt(0);
            return args.Count == this.numArgs;
        }

        public TerminalCommandEX() {
        }

        public static TerminalCommandEX Create() {
            return CreateInstance<TerminalCommandEX>();
        }

        public string command;

        public int numArgs;

        public string description;

        public string parameterDescription;

        public string example;

        public string[] argTips;

        public string actionMethodName;

        public Func<List<string>, IEnumerator> actionMethod;

        public bool argsOptional;

        public bool hidden;

        public static implicit operator TerminalCommandEX(TerminalCommand cmd) {
            var tex = TerminalCommandEX.Create();
            tex.command = cmd.command;
            tex.numArgs = cmd.numArgs;
            tex.description = cmd.description;
            tex.parameterDescription = cmd.parameterDescription;
            tex.example = cmd.example;
            tex.argTips = cmd.argTips;
            tex.actionMethodName = cmd.actionMethod;
            tex.argsOptional = cmd.argsOptional;
            tex.hidden = cmd.hidden;

            return tex;
        }

        public static implicit operator TerminalCommand(TerminalCommandEX tex) {
            var cmd = CreateInstance<TerminalCommand>();
            cmd.command = tex.command;
            cmd.numArgs = tex.numArgs;
            cmd.description = tex.description;
            cmd.parameterDescription = tex.parameterDescription;
            cmd.example = tex.example;
            cmd.argTips = tex.argTips;
            cmd.actionMethod = tex.actionMethodName;
            cmd.argsOptional = tex.argsOptional;
            cmd.hidden = tex.hidden;

            return cmd;
        }
    }
}
