using System.Linq;

namespace SubTerminalEX.Patches {

    [HarmonyPatch(typeof(TerminalInterpreter), nameof(TerminalInterpreter.Interpret))]
    internal static class TerminalInterpreterInterpretPatches {
        // patch field
        internal static bool _fieldCached = false;
        internal static AccessTools.FieldRef<TerminalInterpreter, List<string>> tiArgs;
        internal static MethodInfo closestCmd;
        internal static AudioClip errorClip;

        private static void Cache(TerminalInterpreter term) {
            tiArgs = AccessTools.FieldRefAccess<TerminalInterpreter, List<string>>("args");
            closestCmd = AccessTools.Method("TerminalInterpreter:GetClosestCommand");
            errorClip = term.errorClip;

            _fieldCached = true;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)] // there's no compat here, so it will be broken at some point
        [HarmonyWrapSafe]
        public static bool Prefix(ref string userInput, ref TerminalInterpreter __instance) {
            if (!_fieldCached) Cache(__instance);

            ref List<string> args = ref tiArgs.Invoke(__instance); // woah, hacker man!

            PLog.Inf($"Running: {userInput}");

            args.Clear();
            args.AddRange(userInput.Split([' '], StringSplitOptions.RemoveEmptyEntries)); // yes

            if (args.Count == 0) {
                return false;
            }

            PLog.Inf($"Args: {args.Count}");

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

            PLog.Inf($"Args: {args.Count}");

            if (!TerminalCommandManager._cdict.TryGetValue(args[0], out var cmd)) {
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

            PLog.Inf("Command valid, running it method");
            PLog.Inf(cmd.command);

            if (!cmd.argsOptional && !cmd.CheckArgs(args)) {
                TerminalManager.instance.QueueTextLine("Incorrect use of parameters.  Use \"HELP\" for more info.");
                TerminalManager.instance.QueueTextLine("Expected parameters and usage example:", new Vector2(1f, 1f));
                TerminalManager.instance.QueueTextLine(cmd.command);
                TerminalManager.instance.QueueTextLine("                Parameters:   " + cmd.parameterDescription);
                TerminalManager.instance.QueueTextLine("                Example:      " + cmd.example, new Vector2(0f, 1f));

                return false;
            }

            // before that we strip it command

            if (args.Count > 0 && args[0] == cmd.command)
                args.RemoveAt(0);

            if (cmd.actionMethod != null) {
                __instance.StartCoroutine(cmd.actionMethod.Invoke(args));
            } else {
                __instance.StartCoroutine(cmd.actionMethodName); // why they use this?, i dont know and i dont care
            }

            return false;
        }
    }
}