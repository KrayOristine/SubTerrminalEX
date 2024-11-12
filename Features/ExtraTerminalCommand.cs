using SubTerminalEX.Patches;
using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static UnityEngine.GraphicsBuffer;

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

        internal static readonly HashSet<string> _aliasList = new(); // list of newly created argB
        internal static IEnumerator ProcessAliasFile(string path, int size) {
            const int DEFER_MARK = 1024 * 1024 * 16; // mark whereas after this point will yield pause
            const int UNSAFE_MARK = 1024 * 1024 * 32; // mark whereas file bigger than this will be read "slowly"

            // parse like a token parser
            var zt = 0; // operation done

            string[] str;

            if (size < UNSAFE_MARK) {
                str = File.ReadAllText(path, Encoding.UTF8).Split(['\n', ','], StringSplitOptions.RemoveEmptyEntries);

                if (size > DEFER_MARK) yield return new WaitForEndOfFrame();
            } else {
                // fuck you
                const int READ_AMOUNT = 1024 * 4; // should be within os cache rate

                var sb = new StringBuilder();
                var std = File.Open(path, new FileStreamOptions() { Access = FileAccess.Read, Mode = FileMode.Open, Share = FileShare.None, BufferSize = READ_AMOUNT, Options = FileOptions.SequentialScan });
                var arr = new byte[READ_AMOUNT];

                while (true) {
                    var read = std.Read(arr, 0, READ_AMOUNT);

                    for (int i = 0; i < read; i++) {
                        sb.Append(arr[i]);
                    }

                    Array.Clear(arr);

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

                    argA = args[0];
                    var root = Environment.CurrentDirectory;
                    var target = Path.Combine(root, $"{argA}.txt");

                    // now to the fun shit
                    var std = File.Open(target, new FileStreamOptions() { Access = FileAccess.Write, Mode = FileMode.Create, Options = FileOptions.SequentialScan, Share = FileShare.None });
                    var writer = new StreamWriter(std, Encoding.UTF8);

                    foreach (var alias in _aliasList) {
                        var origin = TerminalCommandManager._cdict[alias]._alias_original; // yeah, welp

                        writer.Write(origin);
                        writer.Write(' ');
                        writer.Write(alias);
                        writer.Write(',');
                    }

                    writer.Flush();
                    std.Seek(1, SeekOrigin.End);
                    std.WriteByte(0);
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

                    Plugin.instance.StartCoroutine(ProcessAliasFile(target, (int)fsize));

                    term.inputEnabled = false;

                    break;

                default:
                    if (args.Count < 2) {
                        term.QueueTextLine($"ERROR: mapping command require 2 argument, received {args.Count}");

                        yield break;
                    }

                    argA = args[1]; // base
                    argB = args[2]; // target

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

        internal static void ShowHelpPage(int pageIndex, int commandsPerPage) {
            var clist = TerminalCommandManager._clist;
            var cLen = clist.Count;
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

        internal static IEnumerator NewHelp(List<string> args) {
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
        }

        internal static IEnumerator NewUpgradeShop(List<string> args) {
            const int num = 8;
            const float numF = 8f;
            int num2 = 0;
            int num3 = Mathf.CeilToInt(UpgradeManager.instance.catalog.items.Length / numF);
            if (args.Count > 0) {
                try {
                    num2 = int.Parse(args[0]) - 1;
                } catch {
                    num2 = -1;
                }
            }
            if (num2 < 0 || num2 > num3 - 1) {
                TerminalManager.instance.QueueTextLine($"ERROR: \"{args[0]}\" is not a valid page.", new Vector2(1f, 1f), 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }

            TerminalManager.instance.QueueTextLine($"UPGRADE CATALOG PAGE {num2+1} OF {num3}", new Vector2(1f, 0f));
            TerminalManager.instance.QueueTextLine("-------------------------------------------------------------------", new Vector2(0f, 1f));
            int num4 = num2 * num;
            while (num4 < (num2 + 1) * num && num4 <= UpgradeManager.instance.catalog.items.Length - 1) {
                UpgradeShopItem upgradeShopItem = UpgradeManager.instance.catalog.items[num4];
                string text = "PRICE: " + UpgradeManager.instance.GetItemPrice(upgradeShopItem).ToString() + " BUCKS";
                if (UpgradeManager.instance.unlockedUpgrades.Contains(upgradeShopItem.upgrade)) {
                    text = "PURCHASED";
                }
                TerminalManager.instance.QueueTextLine($"[{upgradeShopItem.upgradeName.ToUpper()}] {new string('-', 30 - upgradeShopItem.upgradeName.Length)} {text}");
                num4++;
            }
            TerminalManager.instance.QueueTextLine("-------------------------------------------------------------------");
            if (num2 < num3 - 1) {
                TerminalManager.instance.QueueTextLine($"Type \"UPGRADES {num2+2}\" to go to the next page.", new Vector2(0f, 1f));
            }
            TerminalManager.instance.QueueTextLine($"Your Balance: {GameController.instance.balance} BUCKS");
            TerminalManager.instance.QueueTextLine("");
            yield break;
        }

        internal static IEnumerator NewUpgradeBuy(List<string> args) {
            UpgradeShopItem? upgradeShopItem = null;
            if (!GameController.instance.gameStarted) {
                TerminalManager.instance.QueueTextLine("ERROR: Upgrades cannot be bought before initial departure.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            if (args.Count == 0) {
                TerminalManager.instance.QueueTextLine("ERROR: Please specify the upgrade ID you wish to purchase, use command \"UPGRADES\" to view catalog.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }

            foreach (var upgradeShopItem2 in from UpgradeShopItem upgradeShopItem2 in UpgradeManager.instance.catalog.items
                                             where upgradeShopItem2.upgradeName.ToUpper().StartsWith(args[0].Replace('_', ' '))
                                             select upgradeShopItem2) {
                upgradeShopItem = upgradeShopItem2;
                break;
            }

            if (upgradeShopItem == null) {
                TerminalManager.instance.QueueTextLine($"ERROR: Upgrade [{args[0]} not found, use command \"UPGRADES\" to view catalog.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            if (UpgradeManager.instance.unlockedUpgrades.Contains(upgradeShopItem.upgrade)) {
                TerminalManager.instance.QueueTextLine($"ERROR: Upgrade [{args[0]}] has already been purchased.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            if (upgradeShopItem.upgrade == UpgradeManager.Upgrades.StorageCar && TrainCarManager.instance.cars.Count >= TrainCarManager.instance.maxCars) {
                TerminalManager.instance.QueueTextLine("ERROR: Train car limit has been reached.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            int itemPrice = UpgradeManager.instance.GetItemPrice(upgradeShopItem);
            if (itemPrice > GameController.instance.balance) {
                TerminalManager.instance.QueueTextLine("ERROR: Insufficient funds. The A.S.A does not give free handouts.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            StatisticsManager.instance.ChangePlayerValue("totalSpend", itemPrice);
            AchievementManager.instance.ChangeStat("total_spend", itemPrice, true);
            GameController.instance.ChangeBalance(-itemPrice, false);
            TerminalManager.instance.QueueTextLine($"Purchased {upgradeShopItem.upgradeName} upgrade for {itemPrice} BUCKS.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.buyClip, 0.7f);
            TerminalManager.instance.QueueTextLine($"Remaining Balance: {GameController.instance.balance} BUCKS", new Vector2(0f, 1f));
            UpgradeManager.instance.Unlock(upgradeShopItem.upgrade);
            yield return new WaitForEndOfFrame();
            yield break;
        }

        internal static IEnumerator NewHatShop(List<string> args) {
            const int num = 10;
            const float numF = 10f;
            int num2 = 0;
            int num3 = Mathf.CeilToInt(ShopkeeperController.instance.items.Count / numF);
            if (args.Count > 0) {
                try {
                    num2 = int.Parse(args[0]) - 1;
                } catch {
                    num2 = -1;
                }
            }
            if (num2 < 0 || num2 > num3 - 1) {
                TerminalManager.instance.QueueTextLine($"ERROR: \"{args[0]}\" is not a valid page.", new Vector2(1f, 1f), 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            TerminalManager.instance.QueueTextLine($"HAT CATALOG PAGE {num2+1} OF {num3}", new Vector2(1f, 0f));
            TerminalManager.instance.QueueTextLine("-------------------------------------------------------------------", new Vector2(0f, 1f));
            int num4 = num2 * num;
            while (num4 < (num2 + 1) * num && num4 <= ShopkeeperController.instance.items.Count - 1) {
                GameObject gameObject = ShopkeeperController.instance.items[num4];
                string text = $"PRICE: 5 BUCKS";
                if (ShopkeeperController.instance.boughtItems.Contains(gameObject.name)) {
                    text = "PURCHASED";
                }

                TerminalManager.instance.QueueTextLine($"[{gameObject.name.ToUpper().Replace("HAT_", "")}] {new string('-', 30  - gameObject.name.Length)} {text}", Vector2.right);
                num4++;
            }
            TerminalManager.instance.QueueTextLine("-------------------------------------------------------------------", default(Vector2));
            if (num2 < num3 - 1) {
                TerminalManager.instance.QueueTextLine($"Type \"HATS {num2+2}\" to go to the next page.", new Vector2(0f, 1f));
            }
            TerminalManager.instance.QueueTextLine($"Your Balance: {GameController.instance.balance} BUCKS", default(Vector2));
            TerminalManager.instance.QueueTextLine("", default(Vector2));
            yield break;
        }

        internal static IEnumerator NewHatBuy(List<string> args) {
            string text = "";
            if (args.Count == 0) {
                TerminalManager.instance.QueueTextLine("ERROR: Please specify the hat you wish to purchase, use command \"HATS\" to view catalog.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }

            foreach (var gameObject in from GameObject gameObject in ShopkeeperController.instance.items
                                       where gameObject.name.ToUpper().Replace("HAT_", "").StartsWith(args[0].Replace('_', ' '))
                                       select gameObject) {
                text = gameObject.name;
                break;
            }

            if (text == "") {
                TerminalManager.instance.QueueTextLine($"ERROR: Hat [{args[0]}] not found, use command \"HATS\" to view catalog.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            if (ShopkeeperController.instance.boughtItems.Contains(text)) {
                TerminalManager.instance.QueueTextLine($"ERROR: Hat [{args[0]}] is out of stock.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            int num = 5;
            if (num > GameController.instance.balance) {
                TerminalManager.instance.QueueTextLine("ERROR: Insufficient funds. The A.S.A does not give free handouts.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            if (ShopkeeperController.instance.currentHatIndex != -1) {
                TerminalManager.instance.QueueTextLine("ERROR: Shopkeeper is busy, wait your turn.", Vector2.one, 0.075f, TerminalInterpreterInterpretPatches.errorClip, 1f);
                yield break;
            }
            StatisticsManager.instance.ChangePlayerValue("totalSpend", num);
            AchievementManager.instance.ChangeStat("total_spend", num, true);
            GameController.instance.ChangeBalance(-num, false);
            TerminalManager.instance.QueueTextLine($"Purchased {text.ToUpper()} hat for {num} BUCKS.", new Vector2(1f, 1f), 0.075f, TerminalInterpreterInterpretPatches.buyClip, 0.7f);
            TerminalManager.instance.QueueTextLine($"Remaining Balance: {GameController.instance.balance} BUCKS", new Vector2(0f, 1f));
            ShopkeeperController.instance.GiveHat(text);
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
                                                        CommandClear);
            TerminalCommandManager.AliasCommand("clear", "cls");
            _TerminalManager_lastDestroyedLine = AccessTools.FieldRefAccess<TerminalManager, int>("lastDestroyedLine");

            // argB system
            TerminalCommandManager.AddCommand("Alias system",
                                                        "Create terminal alias",
                                                        "alias",
                                                        1,
                                                        false,
                                                        "[kind] [args]",
                                                        [""],
                                                        "alias set view_cam vcam",
                                                        CommandAlias);

            // override help with our new help
            TerminalCommandManager.OverrideCommand("help", NewHelp);

            /*
            TerminalCommandManager.AddCommand("Open upgrade shop",
                                                        "Open upgrade shop",
                                                        "upgrades",
                                                        1,
                                                        true,
                                                        "[page]",
                                                        [""],
                                                        "upgrades 1",
                                                        NewUpgradeShop);

            TerminalCommandManager.AddCommand("Buy upgrade",
                                                        "Buy upgrade",
                                                        "buy_upgrade",
                                                        1,
                                                        true,
                                                        "[name]",
                                                        [""],
                                                        "buy_upgrade helmet_radio",
                                                        NewUpgradeShop);

            TerminalCommandManager.AddCommand("Open hats shop",
                                                        "Open hats shop",
                                                        "hats",
                                                        1,
                                                        true,
                                                        "[page]",
                                                        [""],
                                                        "hats 1",
                                                        NewUpgradeShop);

            TerminalCommandManager.AddCommand("Buy hats",
                                                        "Buy hats",
                                                        "buy_hat",
                                                        1,
                                                        true,
                                                        "[name]",
                                                        [""],
                                                        "buy_hat NotHat",
                                                        NewUpgradeShop);
            */
        }
    }
}
