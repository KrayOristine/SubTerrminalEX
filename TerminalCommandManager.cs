using HarmonyLib;
using SubTerminalEX.Patches;
using System.Xml.Linq;

namespace SubTerminalEX {

    public static class TerminalCommandManager {
        internal static readonly Dictionary<string, TerminalCommandEX> _cdict = new(); // dict to speed up getting

        internal static readonly List<TerminalCommandEX> _clist = new(); // hold original

        internal static readonly List<TerminalCommandEX> _cextra = new(); // things that added by mods

        internal static readonly Dictionary<string, LinkedList<STEHookData>[]> _chook = new(); // hooked shit

        internal static bool _lastModified = false;

        // ayo, this shit is affected by unity fixed update, dont change it!
        internal static void Update() {
            if (!_lastModified) return;

            UpdateToGame();
        }

        internal static void CacheVanillaCommand(TerminalCommand[] list) {
            foreach (var v in list) {
                if (!_cdict.ContainsKey(v.command)) {
                    _cdict.Add(v.command, v);
                    _clist.Add(v);
                }
            }
        }

        public static bool RemoveCommand(string command) {
            if (_cdict.ContainsKey(command)) {
                var tex = _cdict[command];
                _cdict.Remove(command);
                _clist.Remove(tex); // ayo, unexpected shit might happen bro, be careful
                _cextra.Remove(tex);

                UObject.Destroy(tex);

                return true;
            }

            return false;
        }

        internal static void UpdateToGame() {
            var cmds = TerminalManager.instance.commands;
            CacheVanillaCommand(cmds);

            // grow or reduce
            var old = cmds.Length;
            var newSize = _clist.Count + _cextra.Count;

            if (newSize == old) { // bruh
                _lastModified = false;
                return;
            }

            TerminalCommand[] newArr = new TerminalCommand[newSize];
            var ccount = _clist.Count;
            var ecount = _cextra.Count;
            for (int i = 0; i < ccount; i++) {
                newArr[i] = _clist[i];
            }

            for (int i = 0; i < ecount; i++) {
                newArr[i + ccount] = _cextra[i];
            }

            _lastModified = false;

            TerminalManager.instance.commands = newArr;
        }

        public static bool AddNoArgumentCommand(string name, string description, string command, string example, Func<List<string>, IEnumerator> onInvoke, bool hidden = false) {
            command = command.ToUpper();
            if (_cdict.ContainsKey(command)) return false;

            var tex = TerminalCommandEX.Create();

            tex.hidden = hidden;
            tex.name = name;
            tex.description = description;
            tex.command = command;
            tex.actionMethod = onInvoke;
            tex.example = example;
            tex.actionMethodName = "";
            tex.argsOptional = true;
            tex.numArgs = 0;
            tex.parameterDescription = string.Empty;
            tex.argTips = Array.Empty<string>();

            _cdict.Add(command, tex);
            _cextra.Add(tex);

            _lastModified = true;

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
            _cextra.Add(tex);

            _lastModified = true;

            return true;
        }

        public static bool AliasCommand(string oldCommand, string newAlias) {
            oldCommand = oldCommand.ToUpper();
            newAlias = newAlias.ToUpper();
            if (!_cdict.ContainsKey(oldCommand)) return false;
            if (_cdict.ContainsKey(newAlias)) return false;

            var tex = _cdict[oldCommand];
            var ntex = TerminalCommandEX.Create();

            ntex.command = newAlias;
            ntex._alias_original = oldCommand;
            ntex.hidden = tex.hidden;
            ntex.name = tex.name;
            ntex.description = tex.description;
            ntex.numArgs = tex.numArgs;
            ntex.parameterDescription = tex.parameterDescription;
            ntex.actionMethod = tex.actionMethod;
            ntex.actionMethodName = tex.actionMethodName;
            ntex.argsOptional = tex.argsOptional;
            ntex.argTips = tex.argTips;
            ntex.example = tex.example;

            _cdict.Add(newAlias, ntex);
            _cextra.Add(ntex);

            _lastModified = true;

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

        public static bool HookCommand(string targetCommand, Action<List<string>> func, STEHookTarget hookTarget = STEHookTarget.Default, STEHookPriority priority = STEHookPriority.Default) {
            targetCommand = targetCommand.ToUpper();
            if (!_cdict.ContainsKey(targetCommand)) return false;

            if (_chook.TryGetValue(targetCommand, out var list)) {
                var target = list[(int)hookTarget];

                var data = new STEHookData(func, targetCommand, hookTarget, priority);

                // search it
                var n = target.First;

                if (n == null) {
                    target.AddFirst(data);
                    return true;
                }

                while (true) {
                    if (n.Value.Priority < priority) target.AddBefore(n, data);
                    if (n.Next == null) break;

                    n = n.Next;
                }

                target.AddAfter(n, data);

                return true;
            }

            // welp
            var tex = _cdict[targetCommand];
            tex.hookMethod = (string cmd, List<string> args, STEHookTarget executeTarget) => {
                if (!_chook.TryGetValue(cmd, out var list)) return;

                var exec = list[(int)executeTarget];

                var n = exec.First;
                while (n != null) {
                    try {
                        n.Value.Func.Invoke(args);
                    } catch {
                        PLog.Err($"Exception caught upon executing hook of \"{n.Value.TargetCommand}\" command");
                        if (Plugin.DebugMode) {
                            throw; // we dont throw in normal environ, why yeah dont ask me because i dont know why, my brain just made me do it
                        }
                    }

                    n = n.Next;
                }
            };

            list = new LinkedList<STEHookData>[2];
            var before = new LinkedList<STEHookData>();
            var after = new LinkedList<STEHookData>();

            list[0] = before;
            list[1] = after;

            // lazy at it finest
            list[(int)hookTarget].AddFirst(new STEHookData(func, targetCommand, hookTarget, priority));

            _chook.Add(targetCommand, list);

            return true;
        }
    }
}