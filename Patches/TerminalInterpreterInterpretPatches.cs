using System.Linq;
using System.Runtime.InteropServices;

namespace SubTerminalEX.Patches {

    [HarmonyPatch(typeof(TerminalInterpreter), nameof(TerminalInterpreter.Interpret))]
    internal static class TerminalInterpreterInterpretPatches {
        // patch field
        internal static bool _fieldCached = false;
        internal static AccessTools.FieldRef<TerminalInterpreter, List<string>> tiArgs;
        internal static AudioClip errorClip;
        internal static AudioClip buyClip;
        internal static TerminalInterpreter instance;

        private static void Cache(TerminalInterpreter term) {
            tiArgs = AccessTools.FieldRefAccess<TerminalInterpreter, List<string>>("args");
            errorClip = term.errorClip;
            buyClip = term.buyClip;
            instance = term;

            _fieldCached = true;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyWrapSafe]
        public static bool Prefix(ref string userInput, ref TerminalInterpreter __instance) {
            //TODO: make this become IL patch

            if (!_fieldCached) Cache(__instance);

            PLog.Dbg($"Running: {userInput}");

            ref List<string> args = ref tiArgs.Invoke(__instance); // woah, hacker man!

            args.Clear();

            var split = userInput.TrimStart().Split([' '], StringSplitOptions.RemoveEmptyEntries);

            if (split.Length == 0) return false;

            if (split[0].StartsWith("\"")) {
                TerminalManager.instance.QueueTextLine("ERROR: command can not start with \" character");

                return false;
            }

            args.Add(split[0]);

            int i = 1;
            int ac = split.Length;
            var sb = new StringBuilder();
            while (i < ac) {
                if (split[i].StartsWith("\"")) {
                    var cur = split[i];
                    sb.Append(cur.Substring(1));
                    sb.Append(' ');
                    bool valid = false;
                    // walk until end
                    while(i < ac) {
                        i++;
                        cur = split[i];

                        if (cur.EndsWith("\"")) {
                            sb.Append(cur.Substring(0, cur.Length-1));
                            args.Add(sb.ToString());
                            sb.Clear();
                            valid = true;
                            break;
                        }

                        sb.Append(cur);
                        sb.Append(' ');
                    }

                    if (!valid) {
                        TerminalManager.instance.QueueTextLine("ERROR: never ending string argument detected!");

                        return false;
                    }
                } else {
                    args.Add(split[i]);
                }

                i++;
            }

            args[0] = args[0].Replace("\\", "").Replace("/", "");

            switch (args[0]) {
                case "Y":
                    args[0] = "YES";
                    break;
                case "N":
                    args[0] = "NO";
                    break;
            }

            if (__instance.currentPrompt != TerminalInterpreter.Prompts.None && args[0] != "NO" && args[0] != "YES") {
                __instance.currentPrompt = TerminalInterpreter.Prompts.None;
            }

            PLog.Dbg($"Args: ");
            foreach (var arg in args) PLog.Dbg(arg);

            var commandKey = args[0];
            if (!TerminalCommandManager._cdict.TryGetValue(commandKey, out var cmd)) {
                string? text3 = Shared.GetClosestCommand(args[0]);

                if (text3 != null) {
                    TerminalManager.instance.QueueTextLine("Command not recognized. Did you mean " + text3 + "?", new Vector2(1f, 1f));
                    return false;
                }

                text3 = Shared.GetClosestCommand(userInput);
                if (text3 != null) {
                    TerminalManager.instance.QueueTextLine("Command not recognized. Did you mean " + text3 + "?", new Vector2(1f, 1f));
                    return false;
                }

                TerminalManager.instance.QueueTextLine("Command not recognized. Use \"HELP\" for a list of commands.", new Vector2(1f, 1f));
                return false;
            }

            PLog.Dbg($"Command {commandKey} is valid, running it action method keyed {cmd.Command}");

            if (!cmd.ArgsOptional && !cmd.CheckArgs(args)) {
                TerminalManager.instance.QueueTextLine("Incorrect use of parameters.  Use \"HELP\" for more info.");
                TerminalManager.instance.QueueTextLine("Expected parameters and usage example:", new Vector2(1f, 1f));
                TerminalManager.instance.QueueTextLine(cmd.Command);
                TerminalManager.instance.QueueTextLine("                Parameters:   " + cmd.ParameterDescription);
                TerminalManager.instance.QueueTextLine("                Example:      " + cmd.Example, new Vector2(0f, 1f));

                return false;
            }

            // before that we strip it command
            args.RemoveAt(0);

            if (cmd.ForceExactArgument && args.Count > cmd.NumArgs) {
                args.RemoveRange(cmd.NumArgs, args.Count);
            }

            cmd.HookMethod?.Invoke(cmd.Command, args, STEHookTarget.Before);

            if (cmd.ActionMethod != null) {
                __instance.StartCoroutine(cmd.ActionMethod.Invoke(args)); // use this instead of below
            }

            if (!string.IsNullOrWhiteSpace(cmd.actionMethodName)) {

                __instance.StartCoroutine(cmd.actionMethodName); // why they use this?, i dont know and i dont care
            }

            cmd.HookMethod?.Invoke(cmd.Command, args, STEHookTarget.After);

            return false;
        }
    }
}