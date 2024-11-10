using SubTerminalEX.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTerminalEX.Features
{
    public static class ExtraTerminalCommand
    {
        private static bool _init = false;

        internal static AccessTools.FieldRef<TerminalManager, int> _TerminalManager_lastDestroyedLine;

        internal static void ChangeLastDestroyedLine(TerminalManager term) {
            ref int lastDestroyedLine = ref _TerminalManager_lastDestroyedLine.Invoke(term);
            lastDestroyedLine = 0;
        }

        internal static IEnumerator CommandClear(List<string> _) {
            var term = TerminalManager.instance;

            foreach (object obj in term.messageList.transform) {
                Transform transform = (Transform)obj;
                if (transform.name != "UserInputLine") {
                    UObject.Destroy(transform.gameObject);
                }
            }

            term.messageList.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 160f);
            term.response.Clear();
            ChangeLastDestroyedLine(term);

            term.EnableInput();

            yield break;
        }

        public static IEnumerator CommandAlias(List<string> args) {
            PLog.Inf($"{args.Count}");
            var original = args[0];
            var alias = args[1];
            var term = TerminalManager.instance;

            // make sure original exist
            if (!TerminalCommandManager._cdict.ContainsKey(original)) {
                // display info
                term.QueueTextLine($"Can not bind non-existences command {original}");

                yield break;
            }

            var tex = TerminalCommandManager._cdict[original];
            TerminalCommandManager._cdict.Add(alias, tex);

            term.QueueTextLine($"Successfully bound {original} to {alias}");
        }

        public static void ShowHelpPage(int pageIndex, int commandsPerPage) {
            var cLen = TerminalCommandManager._cdict.Count;
            var clist = TerminalCommandManager._clist;
            int num = Mathf.CeilToInt(cLen / (float)commandsPerPage);
            TerminalManager.instance.QueueTextLine($"PAGE {pageIndex + 1} OF {num}", new Vector2(1f, 1f));
            TerminalManager.instance.QueueTextLine("-------------------------------------------------------------------");
            TerminalManager.instance.QueueTextLine("Available Commands:", new Vector2(1f, 1f));
            int num2 = pageIndex * commandsPerPage;
            while (num2 < (pageIndex + 1) * commandsPerPage && num2 <= cLen - 1) {
                TerminalCommand terminalCommand = clist[num2];
                if (!terminalCommand.hidden) {
                    TerminalManager.instance.QueueTextLine(terminalCommand.command);
                    TerminalManager.instance.QueueTextLine("                          Description:  " + terminalCommand.description);
                    TerminalManager.instance.QueueTextLine("                          Parameters:   " + terminalCommand.parameterDescription);
                    TerminalManager.instance.QueueTextLine("                          Example:      " + terminalCommand.example);
                }
                num2++;
            }

            TerminalManager.instance.QueueTextLine("-------------------------------------------------------------------", new Vector2(1f, 1f));
        }

        public static IEnumerator NewHelp(List<string> args) {
            const int perPage = 5;
            const float perPageF = 5f;
            int num2 = 0;
            int num3 = Mathf.CeilToInt(TerminalCommandManager._cdict.Count / perPageF);
            if (args.Count > 0) {
                try {
                    num2 = int.Parse(args[0]) - 1;
                } catch {
                    num2 = 0;
                }
            }
            if (num2 < 0 || num2 > num3 - 1) {
                TerminalManager.instance.QueueTextLine("ERROR: \"" + args[0] + "\" is not a valid page.", new Vector2(1f, 1f), 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }

            ShowHelpPage(num2, perPage);

            if (num2 < num3 - 1) {
                TerminalManager.instance.QueueTextLine("Type \"HELP " + (num2 + 2).ToString() + "\" to go to the next page.", new Vector2(0f, 1f));
            }
            yield return new WaitForEndOfFrame();
            yield break;
        }


        public static void Register()
        {
            if (_init) return;

            // clear
            TerminalCommandManager.AddNoArgumentCommand("Clear terminal",
                                                        "Clear the terminal",
                                                        "clear",
                                                        "",
                                                        [""],
                                                        "clear",
                                                        CommandClear);
            _TerminalManager_lastDestroyedLine = AccessTools.FieldRefAccess<TerminalManager, int>("lastDestroyedLine");

            // alias
            TerminalCommandManager.AddCommand("Create terminal alias",
                                                        "Create terminal alias",
                                                        "alias",
                                                        2,
                                                        false,
                                                        "[original] [newAlias]",
                                                        [""],
                                                        "alias view_cams vcam",
                                                        CommandAlias);

            // override help with our new help
            TerminalCommandManager.OverrideCommand("help", NewHelp);
        }
    }
}
