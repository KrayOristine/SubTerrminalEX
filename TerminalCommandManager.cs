using HarmonyLib;
using SubTerminalEX.Patches;

namespace SubTerminalEX {

    public static class TerminalCommandManager {
        internal static readonly Dictionary<string, TerminalCommandEX> _cdict = new(); // dict to speed up getting

        internal static readonly List<TerminalCommandEX> _clist = new(); // hold original

        internal static readonly List<TerminalCommandEX> _cextra = new(); // things that added by mods

        internal static readonly Dictionary<string, LinkedList<STEHookData>[]> _chook = new(); // hooked shit

        internal static bool _lastModified = false;

        // ayo, this shit is affected by unity fixed update, dont change it!
        internal static void Update(bool force = false) {
            if (!_lastModified && !force) return;

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

        /// <summary>
        /// Remove a specific command from game
        /// </summary>
        /// <param name="command">command to remove (case in-sensitive)</param>
        /// <returns><see langword="true"/> if able to remove a command, otherwise <see langword="false"/></returns>
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

        /// <summary>
        /// Add command that dont need argument
        /// </summary>
        /// <param name="command">user need to type in this exact keyword (case in-sensitive)</param>
        /// <param name="onInvoke">Func that will be executed upon this command ran</param>
        /// <param name="name">(optional)</param>
        /// <param name="description">(optional)</param>
        /// <param name="example">example how user should use it</param>
        /// <param name="hidden">does this command hidden from user?</param>
        /// <returns><see langword="true"/> if able to add a command, otherwise <see langword="false"/></returns>
        public static bool AddNoArgumentCommand(string command, Func<List<string>, IEnumerator> onInvoke, string name = "", string description = "", string example = "", bool hidden = false) {
            command = command.ToUpper();
            if (_cdict.ContainsKey(command)) return false;

            var tex = TerminalCommandEX.Create();

            tex.Hidden = hidden;
            tex.name = name;
            tex.Description = description;
            tex.Command = command;
            tex.ActionMethod = onInvoke;
            tex.Example = example;
            tex.actionMethodName = "";
            tex.ForceExactArgument = false;
            tex.ArgsOptional = true;
            tex.NumArgs = 0;
            tex.ParameterDescription = string.Empty;
            tex.ArgTips = Array.Empty<string>();

            _cdict.Add(command, tex);
            _cextra.Add(tex);

            _lastModified = true;

            return true;
        }

        /// <summary>
        /// Add a command
        /// </summary>
        /// <param name="name">(optional)</param>
        /// <param name="description">(optional)</param>
        /// <param name="command">user must type in this exact word to execute this command (case in-sensitive)</param>
        /// <param name="argAmount">amount of argument this command may need</param>
        /// <param name="forceExactArgumentCount">make argument passed to this command stripped to the exact amount of argument it need</param>
        /// <param name="argOptional">is argument optional on this command</param>
        /// <param name="parameterDesc">self-explanatory</param>
        /// <param name="argumentTips">self-explanatory</param>
        /// <param name="example">self-explanatory</param>
        /// <param name="onInvoke">self-explanatory</param>
        /// <param name="hidden">is this command hidden from normal user eyes?</param>
        /// <returns><see langword="true"/> if able to add a command, otherwise <see langword="false"/></returns>
        public static bool AddCommand(string command, Func<List<string>, IEnumerator> onInvoke, string name = "", string description = "", int argAmount = 0, bool forceExactArgumentCount = false, bool argOptional = true, string parameterDesc = "", string[]? argumentTips = null, string example = "", bool hidden = false) {
            command = command.ToUpper();
            if (_cdict.ContainsKey(command)) return false;

            var tex = TerminalCommandEX.Create();

            tex.Hidden = hidden;
            tex.name = name;
            tex.Description = description;
            tex.Command = command;
            tex.NumArgs = argAmount;
            tex.ParameterDescription = parameterDesc;
            tex.ActionMethod = onInvoke;
            tex.actionMethodName = "";
            tex.ArgsOptional = argOptional;
            tex.ArgTips = argumentTips ?? [""];
            tex.Example = example;
            tex.ForceExactArgument = forceExactArgumentCount;

            _cdict.Add(command, tex);
            _cextra.Add(tex);

            _lastModified = true;

            return true;
        }

        /// <summary>
        /// Create an alias of an existing command
        /// </summary>
        /// <param name="oldCommand"></param>
        /// <param name="newAlias"></param>
        /// <returns><see langword="true"/> if able to alias a command, otherwise <see langword="false"/></returns>
        public static bool AliasCommand(string oldCommand, string newAlias) {
            oldCommand = oldCommand.ToUpper();
            newAlias = newAlias.ToUpper();
            if (!_cdict.ContainsKey(oldCommand)) return false; // no origin exist
            if (_cdict.ContainsKey(newAlias)) return false; // already have it alias

            var tex = _cdict[oldCommand];
            var ntex = TerminalCommandEX.Create();

            ntex.Command = newAlias;
            ntex._alias_original = oldCommand;
            ntex.Hidden = tex.Hidden;
            ntex.name = tex.name;
            ntex.Description = tex.Description;
            ntex.NumArgs = tex.NumArgs;
            ntex.ParameterDescription = tex.ParameterDescription;
            ntex.ActionMethod = tex.ActionMethod;
            ntex.actionMethodName = tex.actionMethodName;
            ntex.ArgsOptional = tex.ArgsOptional;
            ntex.ArgTips = tex.ArgTips;
            ntex.Example = tex.Example;
            if (!string.IsNullOrWhiteSpace(tex.actionMethodName)) {
                ntex.ForceExactArgument = true;
            }

            _cdict.Add(newAlias, ntex);
            _cextra.Add(ntex);

            _lastModified = true;

            return true;
        }

        /// <summary>
        /// Override existing command with your custom
        /// </summary>
        /// <param name="oldCommand"></param>
        /// <param name="onInvoke"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="argAmount"></param>
        /// <param name="argOptional"></param>
        /// <param name="parameterDesc"></param>
        /// <param name="argumentTips"></param>
        /// <param name="example"></param>
        /// <param name="hidden"></param>
        /// <returns><see langword="true"/> if able to override a command, otherwise <see langword="false"/></returns>
        public static bool OverrideCommand(string oldCommand, Func<List<string>, IEnumerator> onInvoke, string? name = null, string? description = null, int? argAmount = null, bool? argOptional = null, string? parameterDesc = null, string[]? argumentTips = null, string? example = null, bool? hidden = null) {
            oldCommand = oldCommand.ToUpper();
            if (!_cdict.ContainsKey(oldCommand)) return false;

            var tex = _cdict[oldCommand];

            tex.Hidden = hidden ?? tex.Hidden;
            tex.name = name ?? tex.name;
            tex.Description = description ?? tex.Description;
            tex.NumArgs = argAmount ?? tex.NumArgs;
            tex.ParameterDescription = parameterDesc ?? tex.ParameterDescription;
            tex.ActionMethod = onInvoke;
            tex.actionMethodName = string.Empty; // we remove this to prevent shit
            tex.ForceExactArgument = false;
            tex.ArgsOptional = argOptional ?? tex.ArgsOptional;
            tex.ArgTips = argumentTips ?? tex.ArgTips;
            tex.Example = example ?? tex.Example;

            return true;
        }

        /// <summary>
        /// Hook a command so that whenever it run your action would also run!
        /// </summary>
        /// <param name="targetCommand"></param>
        /// <param name="func"></param>
        /// <param name="hookTarget"></param>
        /// <param name="priority"></param>
        /// <returns><see langword="true"/> if able to hook a command, otherwise <see langword="false"/></returns>
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
            tex.HookMethod = (string cmd, List<string> args, STEHookTarget executeTarget) => {
                if (!_chook.TryGetValue(cmd, out var list)) return;

                var exec = list[(int)executeTarget];

                var n = exec.First;
                while (n != null) {
                    try {
                        n.Value.Func.Invoke(args);
                    } catch {
                        PLog.Err($"Exception caught upon executing hook of \"{n.Value.TargetCommand}\" command");
                        if (Plugin.pluginInstance.DebugMode) {
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