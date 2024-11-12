
using BepInEx.Configuration;
using SubTerminalEX.Features;
using System.IO;
using UnityEngine.UIElements.Collections;

namespace SubTerminalEX;

[BepInPlugin(SubterexInfo.PLUG_GUID, SubterexInfo.PLUG_NAME, SubterexInfo.PLUG_VER)]
public sealed class Plugin : BaseUnityPlugin
{
    internal static Harmony? _harmony;
    internal static Plugin instance;

    internal static bool DebugMode = false;

    private void Awake()
    {
        instance = this;

        if (_harmony != null) {
            _harmony.UnpatchSelf();
            _harmony = null;
        }

        if (Config.Count <= 0) {
            SetupConfig();
        }

        PreloadConfig();

        // Plugin startup logic
        PLog.InitLog(base.Logger);

        PLog.Inf($"Loaded successfully - version: {SubterexInfo.PLUG_VER}");

        _harmony = new Harmony(SubterexInfo.PLUG_GUID);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        Startup();
    }

    private static void OnDestroy() {
        if (_harmony == null) return;

        _harmony.UnpatchSelf();
        _harmony = null;
    }

    private static void FixedUpdate() {
        TerminalCommandManager.Update();
    }

    private void Startup() {
        ExtraTerminalCommand.Register();
    }

    internal const string ConfigAliasSection = "Alias Settings";
    internal const string ConfigTerminalSection = "Terminal Settings";
    internal void OnGameStartup() {
        TerminalCommandManager.UpdateToGame();



        if (Config.TryGetEntry<bool>(ConfigAliasSection, "Enable Autoload", out var autoload) && autoload.Value) {
            var root = Environment.CurrentDirectory;
            var target = Path.Combine(root, (string)(Config[ConfigAliasSection, "Autoload File"].BoxedValue));

            var fsize = new FileInfo(target).Length;
            const int OneGig = 1024 * 1024 * 1024;
            if (fsize > OneGig) {
                return;
            }

            if (File.Exists(target)) {
                StartCoroutine(ExtraTerminalCommand.ProcessAliasFile(target, (int)fsize));
            }
        }
    }

    internal void PreloadConfig() {
        // more like cache but meh

        var entry = Config["Debug", "Debug Mode"];
        DebugMode = entry.BoxedValue != null ? (bool)entry.BoxedValue : (bool)entry.DefaultValue;

    }


    internal void SetupConfig() {

        // auto load
        Config.Bind(ConfigAliasSection, "Enable AutoLoad", false, "Allow this mod to automatically load your alias definition upon game start");
        Config.Bind(ConfigAliasSection, "AutoLoad File", "aliasAutoLoad", "The target file name to load from");

        // fast ConfigTerminalSection
        Config.Bind(ConfigTerminalSection, "Skip Bootup Text", true, "Skip terminal bootup text");

        //dev section
        Config.Bind("Debug", "Debug Mode", false, "Turn on debug mode for mods developer");

        Config.Save();
    }
}
