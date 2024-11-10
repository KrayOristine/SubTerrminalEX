
using SubTerminalEX.Patches;

namespace SubTerminalEX {
    public static class TerminalCommandManager {
        internal static readonly Dictionary<string, TerminalCommandEX> _cdict = new();
        internal static readonly List<TerminalCommandEX> _clist = new();

        internal static readonly List<TerminalCommandEX> _cextra = new(); // things that added by mods


        internal static void CacheVanillaCommand(TerminalCommand[] list) {
            foreach (var v in list) {
                if (!_cdict.ContainsKey(v.command)) {
                    _cdict.Add(v.command, v);
                    _clist.Add(v);
                }
            }
        }

        public static void RemoveCommand(string command) {
            if (_cdict.ContainsKey(command)) {
                var tex = _cdict[command];
                _cdict.Remove(command);
                _clist.Remove(tex);
            }
        }

        internal static void AddToGame() {
            var cmds = TerminalManager.instance.commands;
            CacheVanillaCommand(cmds);

            // we grow array
            var old = cmds.Length;
            var newSize = old + _cextra.Count;
            var newArr = new TerminalCommand[newSize];

            for (int i = 0; i < old; i++) {
                newArr[i] = cmds[i];
            }

            for (int i = old; i < newSize; i++) {
                newArr[i] = _cextra[i - old];
            }

            TerminalManager.instance.commands = newArr;
        }

        public static bool AddNoArgumentCommand(string name, string description, string command, string parameterDesc, string[] argumentTips, string example, Func<List<string>, IEnumerator> onInvoke, bool hidden = false) {
            command = command.ToUpper();
            if (_cdict.ContainsKey(command)) return false;

            var tex = TerminalCommandEX.Create();

            tex.hidden = hidden;
            tex.name = name;
            tex.description = description;
            tex.command = command;
            tex.numArgs = 0;
            tex.parameterDescription = parameterDesc;
            tex.actionMethod = onInvoke;
            tex.actionMethodName = "";
            tex.argsOptional = true;
            tex.argTips = argumentTips;
            tex.example = example;

            _cdict.Add(command, tex);
            _clist.Add(tex);
            _cextra.Add(tex);

            return true;
        }

        public static bool AddCommand(string name, string description, string command, int argAmount, bool argOptional, string parameterDesc, string[] argumentTips, string example, Func<List<string>, IEnumerator> onInvoke, bool hidden = false) {
            command = command.ToUpper();
            if (_cdict.ContainsKey(command)) return false;

            var tex = TerminalCommandEX.Create();

            tex.hidden = hidden;
            tex.name = name;
            tex.description = description;
            tex.command = command;
            tex.numArgs = argAmount;
            tex.parameterDescription = parameterDesc;
            tex.actionMethod = onInvoke;
            tex.actionMethodName = "";
            tex.argsOptional = argOptional;
            tex.argTips = argumentTips;
            tex.example = example;

            _cdict.Add(command, tex);
            _clist.Add(tex);
            _cextra.Add(tex);

            return true;
        }

        public static bool OverrideCommand(string oldCommand, Func<List<string>, IEnumerator> onInvoke, string? name = null, string? description = null, int? argAmount = null, bool? argOptional = null, string? parameterDesc = null, string[]? argumentTips = null, string? example = null, bool? hidden = null) {
            oldCommand = oldCommand.ToUpper();
            if (!_cdict.ContainsKey(oldCommand)) return false;

            var tex = _cdict[oldCommand];

            tex.hidden = hidden ?? tex.hidden;
            tex.name = name ?? tex.name;
            tex.description = description ?? tex.description;
            tex.numArgs = argAmount ?? tex.numArgs;
            tex.parameterDescription = parameterDesc ?? tex.parameterDescription;
            tex.actionMethod = onInvoke;
            tex.actionMethodName = string.Empty; // we remove this to prevent shit
            tex.argsOptional = argOptional ?? tex.argsOptional;
            tex.argTips = argumentTips ?? tex.argTips;
            tex.example = example ?? tex.example;

            return true;
        }
}
}
