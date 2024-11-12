

namespace SubTerminalEX {
    public class TerminalCommandEX : ScriptableObject
    {
        public bool CheckArgs(List<string> args) {
            return args.Count - 1 == numArgs;
        }

        public TerminalCommandEX() {
        }

        public static TerminalCommandEX Create() {
            return CreateInstance<TerminalCommandEX>();
        }

        public string command = string.Empty;

        public string? _alias_original = string.Empty;

        public int numArgs = 0;

        public string description = string.Empty;

        public string parameterDescription = string.Empty;

        public string example = string.Empty;

        public string[] argTips = Array.Empty<string>();

        public string actionMethodName = string.Empty;

        public Func<List<string>, IEnumerator>? actionMethod;

        public Action<string, List<string>, STEHookTarget>? hookMethod;

        public bool argsOptional = true;

        public bool hidden = false;

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
