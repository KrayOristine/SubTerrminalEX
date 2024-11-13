using SubTerminalEX.Patches;
using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static UnityEngine.GraphicsBuffer;
using System.Runtime.InteropServices;

namespace SubTerminalEX.Features
{
    internal static class ExtraTerminalCommand
    {
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

        internal static readonly HashSet<string> _aliasList = new(); // list of newly created argB
        internal static string _palias_path = string.Empty;
        internal static int _palias_size = 0;
        internal static IEnumerator ProcessAliasFile() {
            const int DEFER_MARK = 1024 * 1024 * 16; // mark whereas after this point will yield pause
            const int UNSAFE_MARK = 1024 * 1024 * 32; // mark whereas file bigger than this will be read "slowly"

            // parse like a token parser
            var zt = 0; // operation done

            string[] str;

            if (_palias_size < UNSAFE_MARK) {
                str = File.ReadAllText(_palias_path, Encoding.UTF8).Split(['\n', ','], StringSplitOptions.RemoveEmptyEntries);

                if (_palias_size > DEFER_MARK) yield return new WaitForEndOfFrame();
            } else {
                // fuck you
                const int READ_AMOUNT = 1024 * 4; // should be within os cache rate

                var sb = new StringBuilder();
                var std = File.Open(_palias_path, FileMode.Open, FileAccess.Read, FileShare.None);
                var arr = new byte[READ_AMOUNT];

                while (true) {
                    var read = std.Read(arr, 0, READ_AMOUNT);

                    for (int i = 0; i < read; i++) {
                        sb.Append(arr[i]);
                    }

                    Array.Clear(arr, 0, read);

                    if (read < READ_AMOUNT) {
                        break;
                    }

                    zt++;

                    if (zt >= 10) { // wait for next frame
                        zt = 0;
                        yield return new WaitForEndOfFrame();
                    }
                }

                str = sb.ToString().Split(['\n', ','], StringSplitOptions.RemoveEmptyEntries);

                sb.Clear();
                std.Dispose();

                yield return new WaitForEndOfFrame();
            }
            zt = 0;
            foreach (var item in str) {
                if (zt >= 100) {
                    zt = 0;
                    yield return new WaitForEndOfFrame();
                }

                if (string.IsNullOrWhiteSpace(item)) continue;

                var data = item.Split(' ');
                if (data.Length < 2) continue; // hell nah

                var start = data[0] == " " ? 1 : 0;
                var og = data[start];
                var tg = data[start + 1];

                if (TerminalCommandManager.AliasCommand(data[start], data[start + 1])) {
                    _aliasList.Add(data[start + 1]);
                }

                zt++;
            }

            var term = TerminalManager.instance;

            term.QueueTextLine("Successfully loaded alias file");
            term.EnableInput();
        }

        internal static IEnumerator CommandAlias(List<string> args) {
            var kind = args[0];
            var term = TerminalManager.instance;
            switch (kind) {

                case "SET":
                case "MAP":
                    if (args.Count < 3) {
                        term.QueueTextLine($"ERROR: mapping command require 2 argument, received {args.Count-1}");

                        yield break;
                    }

                    var argA = args[1]; // base
                    var argB = args[2]; // target

                    if (!TerminalCommandManager._cdict.ContainsKey(argA)) {
                        term.QueueTextLine($"ERROR: Can not map to non-existences command \"{argA}\"");

                        yield break;
                    }

                    if (TerminalCommandManager.AliasCommand(argA, argB)) {
                        _aliasList.Add(argB);
                    }

                    term.QueueTextLine($"Successfully map \"{argB}\" to \"{argA}\"");
                    break;

                case "REMOVE":
                case "REM":
                case "RM":
                    if (args.Count < 2) {
                        term.QueueTextLine($"ERROR: remove mapped alias command require 1 argument, received 0");

                        yield break;
                    }

                    argA = args[1];

                    if (!_aliasList.Contains(argA)) {
                        term.QueueTextLine($"ERROR: Can not remove non-existences alias \"{argA}\"");

                        yield break;
                    }

                    _aliasList.Remove(argA);

                    TerminalCommandManager.RemoveCommand(argA);
                    term.QueueTextLine($"Successfully removed alias \"{argA}\"");

                    break;

                case "CLEAR":
                case "RESET":
                    if (_aliasList.Count <= 0) {
                        term.QueueTextLine($"ERROR: Can not clear alias because none exists");

                        yield break;
                    }
                    int i = (from item in _aliasList
                             where TerminalCommandManager.RemoveCommand(item)
                             select item).Count();

                    term.QueueTextLine($"Successfully removed {i} mapped alias");


                    break;

                case "GEN":
                case "GENERATE":
                    if (args.Count < 2) {
                        term.QueueTextLine($"ERROR: generate alias file require 1 argument, received 0");

                        yield break;
                    }

                    argA = args[1];
                    var root = Environment.CurrentDirectory;
                    var target = Path.Combine(root, $"{argA}.txt");

                    // now to the fun shit
                    var std = File.Open(target, FileMode.Create, FileAccess.Write, FileShare.None);
                    var writer = new StreamWriter(std, Encoding.UTF8);

                    foreach (var alias in _aliasList) {
                        var origin = TerminalCommandManager._cdict[alias]._alias_original; // yeah, welp

                        writer.Write(origin);
                        writer.Write(' ');
                        writer.Write(alias);
                        writer.Write(',');
                        writer.Flush();
                    }

                    std.SetLength(std.Length - 1);
                    std.Flush();

                    writer.Close();

                    term.QueueTextLine($"Successfully wrote to {argA}.txt");

                    break;

                case "LOAD":
                    if (args.Count < 2) {
                        term.QueueTextLine($"ERROR: load alias file require 1 argument, received 0");

                        yield break;
                    }

                    argA = args[1];

                    // cur
                    root = Environment.CurrentDirectory;
                    target = Path.Combine(root, $"{argA}.txt");

                    if (!File.Exists(target)) {
                        term.QueueTextLine($"ERROR: Can not load non-existence alias file");

                        yield break;
                    }

                    const int LIMIT = 1024 * 1024 * 1024;  // seriously, why someone will make an alias file this big?
                    var fsize = new FileInfo(target).Length;
                    if (fsize > LIMIT) {
                        // fuck you 1
                        term.QueueTextLine($"ERROR: Will not load alias file larger than 1GB");

                        yield break;
                    }

                    // we clean the alias before load up new one
                    if (_aliasList.Count > 0)
                        foreach (var item in _aliasList)
                            TerminalCommandManager.RemoveCommand(item);

                    term.QueueTextLine($"Loading pre-defined alias text file named: {argA}.txt");
                    term.QueueTextLine("WARN: This operation may took a long time");

                    _palias_path = target;
                    _palias_size = (int)fsize;
                    Plugin.Interpreter.StartCoroutine(ProcessAliasFile());

                    term.inputEnabled = false;

                    break;

                default:
                    if (args.Count < 2) {
                        term.QueueTextLine($"ERROR: mapping command require 2 argument, received {args.Count}");

                        yield break;
                    }

                    argA = args[0]; // base
                    argB = args[1]; // target

                    if (!TerminalCommandManager._cdict.ContainsKey(argA)) {
                        term.QueueTextLine($"ERROR: Can not map to non-existences command \"{argA}\"");

                        yield break;
                    }

                    if (TerminalCommandManager.AliasCommand(argA, argB)) {
                        _aliasList.Add(argB);
                    }

                    term.QueueTextLine($"Successfully map \"{argB}\" to \"{argA}\"");
                    break;
            }
        }

        internal static MethodInfo _hatlist;
        internal static MethodInfo _hatbuy;
        internal static MethodInfo _upgradelist;
        internal static MethodInfo _upgradebuy;
        internal static IEnumerator HatShop(List<string> arg) {
            return (IEnumerator)_hatlist.Invoke(TerminalInterpreterInterpretPatches.instance, null);
        }

        internal static IEnumerator HatBuy(List<string> arg) {
            return (IEnumerator)_hatbuy.Invoke(TerminalInterpreterInterpretPatches.instance, null);
        }

        internal static IEnumerator UpgradeShop(List<string> arg) {
            return (IEnumerator)_upgradelist.Invoke(TerminalInterpreterInterpretPatches.instance, null);
        }

        internal static IEnumerator UpgradeBuy(List<string> arg) {
            return (IEnumerator)_upgradebuy.Invoke(TerminalInterpreterInterpretPatches.instance, null);
        }


        internal static void Register()
        {

            // clear
            TerminalCommandManager.AddNoArgumentCommand("clear",
                                                        CommandClear,
                                                        "Clear terminal",
                                                        "Clear the terminal",
                                                        ""
                                                        );
            TerminalCommandManager.AliasCommand("clear", "cls");
            _TerminalManager_lastDestroyedLine = AccessTools.FieldRefAccess<TerminalManager, int>("lastDestroyedLine");

            // argB system
            TerminalCommandManager.AddCommand("alias", CommandAlias,
                                                        "Alias system",
                                                        "Create terminal alias",
                                                        1,
                                                        false,
                                                        false,
                                                        "[kind] [args]",
                                                        [""],
                                                        "alias set view_cam vcam");

            // hats and upgrades 'hacks'

            _hatlist = AccessTools.Method("TerminalInterpreter:OpenHatShopCommand");
            _hatbuy = AccessTools.Method("TerminalInterpreter:BuyHatCommand");
            _upgradelist = AccessTools.Method("TerminalInterpreter:OpenUpgradeShopCommand");
            _upgradebuy = AccessTools.Method("TerminalInterpreter:BuyUpgradeCommand");

            TerminalCommandManager.AddCommand("hats", HatShop);

            TerminalCommandManager.AddCommand("buy_hat", HatBuy, argAmount: 1, forceExactArgumentCount: true);

            TerminalCommandManager.AddCommand("upgrades", UpgradeShop);

            TerminalCommandManager.AddCommand("buy_upgrade", UpgradeBuy, argAmount: 1, forceExactArgumentCount: true);
        }
    }
}
