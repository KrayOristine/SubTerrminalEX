

namespace SubTerminalEX {
    /// <summary>
    /// Extended Terminal Command Object (ETCO)
    /// </summary>
    public class TerminalCommandEX : ScriptableObject
    {
        internal bool CheckArgs(List<string> args) {
            if (ForceExactArgument)
                return args.Count - 1 == NumArgs;
            else
                return args.Count - 1 >= NumArgs;
        }

        /// <summary>
        /// Internal only
        /// </summary>
        internal TerminalCommandEX() {
        }

        internal static TerminalCommandEX Create() {
            return CreateInstance<TerminalCommandEX>();
        }

        /// <summary>
        /// command
        /// </summary>
        public string Command = string.Empty;

        /// <summary>
        /// the original command that this alias point to
        /// </summary>
        internal string? _alias_original = string.Empty;

        /// <summary>
        /// minimum amount of argument this command need
        /// </summary>
        public int NumArgs = 0;

        /// <summary>
        /// like it name
        /// </summary>
        public string Description = string.Empty;

        /// <summary>
        /// like it name
        /// </summary>
        public string ParameterDescription = string.Empty;

        /// <summary>
        /// like it name
        /// </summary>
        public string Example = string.Empty;

        /// <summary>
        /// i dont know and i dont care
        /// </summary>
        public string[] ArgTips = Array.Empty<string>();

        /// <summary>
        /// string that will be executed, preserved for vanilla (you should not modify this)
        /// </summary>
        internal string actionMethodName = string.Empty;

        /// <summary>
        /// action that execute upon command ran
        /// </summary>
        public Func<List<string>, IEnumerator>? ActionMethod;

        /// <summary>
        /// hook action that execute on command ran
        /// </summary>
        public Action<string, List<string>, STEHookTarget>? HookMethod;

        /// <summary>
        /// is argument optional for this command? (ignore argument check)
        /// </summary>
        public bool ArgsOptional = true;

        /// <summary>
        /// is this command hidden for normal user (dont show up)
        /// </summary>
        public bool Hidden = false;

        /// <summary>
        /// force interpreter to strip argument down to exactly <see cref="TerminalCommandEX.NumArgs"/>
        /// </summary>
        public bool ForceExactArgument = true;

        public static implicit operator TerminalCommandEX(TerminalCommand cmd) {
            var tex = TerminalCommandEX.Create();
            tex.Command = cmd.command;
            tex.NumArgs = cmd.numArgs;
            tex.Description = cmd.description;
            tex.ParameterDescription = cmd.parameterDescription;
            tex.Example = cmd.example;
            tex.ArgTips = cmd.argTips;
            tex.actionMethodName = cmd.actionMethod;
            tex.ArgsOptional = cmd.argsOptional;
            tex.Hidden = cmd.hidden;

            return tex;
        }

        public static implicit operator TerminalCommand(TerminalCommandEX tex) {
            var cmd = CreateInstance<TerminalCommand>();
            cmd.command = tex.Command;
            cmd.numArgs = tex.NumArgs;
            cmd.description = tex.Description;
            cmd.parameterDescription = tex.ParameterDescription;
            cmd.example = tex.Example;
            cmd.argTips = tex.ArgTips;
            cmd.actionMethod = tex.actionMethodName;
            cmd.argsOptional = tex.ArgsOptional;
            cmd.hidden = tex.Hidden;

            return cmd;
        }
    }
}
