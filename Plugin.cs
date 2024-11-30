
using BepInEx.Configuration;
using SubTerminalEX.Features;
using System.IO;
using UnityEngine.UIElements.Collections;

namespace SubTerminalEX;

[BepInPlugin(SubterexInfo.PLUG_GUID, SubterexInfo.PLUG_NAME, SubterexInfo.PLUG_VER)]
internal sealed class Plugin : BaseUnityPlugin
{
    internal static Harmony? _harmony;
    internal static Plugin pluginInstance;
    internal static bool GameStarted = false;

    internal bool DebugMode = false;
    internal bool FastTerminalBoot = false;
    internal bool EnableAutoload = false;
    internal string AutoloadTargetFile = string.Empty;

    // fuck me
    internal static MonoBehaviour Interpreter => TerminalManager.instance.interpreter;

    private void Awake()
    {
        pluginInstance = this;

        if (_harmony != null) {
            _harmony.UnpatchSelf();
            _harmony = null;
        }

        SetupConfig();
        CacheConfig();

        PLog.InitLog(Logger);

        PLog.Inf($"Loaded successfully - version: {SubterexInfo.PLUG_VER}");

        _harmony = new Harmony(SubterexInfo.PLUG_GUID);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    private void OnDestroy() {
        if (_harmony == null) return;

        PLog.Wrn("SubTerminalEX is destroyed!, terminating all feature and patches...");

        _harmony.UnpatchSelf();
        _harmony = null;
    }


    internal int ticks = 0;
    private void FixedUpdate() {
        if (!GameStarted) return;
        if (ticks < 10) {
            ticks++;

            return;
        }

        ticks = 0;
        TerminalCommandManager.Update();
    }

    internal const string ConfigAliasSection = "Alias Settings";
    internal const string ConfigTerminalSection = "Terminal Settings";
    internal void OnGameStartup() {
        TerminalCommandManager.CacheVanillaCommand();
        TerminalCommandManager.UpdateToGame();
        ExtraTerminalCommand.Register();
        GameStarted = true;

        if (EnableAutoload) {
            var root = Environment.CurrentDirectory;
            var target = Path.Combine(root, $"{AutoloadTargetFile.ToUpper()}.txt");

            var fsize = new FileInfo(target).Length;
            const int OneGig = 1024 * 1024 * 1024;
            if (fsize > OneGig) {
                return; // i, WHAT THE F***
            }

            if (File.Exists(target)) {
                ExtraTerminalCommand._palias_path = target;
                ExtraTerminalCommand._palias_size = (int)fsize;
                StartCoroutine(ExtraTerminalCommand.ProcessAliasFile());
            }
        }
    }

    internal static T GetValueFromConfigEntry<T>(ConfigEntryBase entry) {
        return entry.BoxedValue != null ? (T)entry.BoxedValue : (T)entry.DefaultValue;
    }

    internal void CacheConfig() {
        // more like cache but meh

        DebugMode = GetValueFromConfigEntry<bool>(Config["Debug", "Debug Mode"]);
        FastTerminalBoot = GetValueFromConfigEntry<bool>(Config[ConfigTerminalSection, "Skip Bootup Text"]);
        EnableAutoload = GetValueFromConfigEntry<bool>(Config[ConfigAliasSection, "Enable Autoload"]);
        AutoloadTargetFile = GetValueFromConfigEntry<string>(Config[ConfigAliasSection, "Autoload File"]);
    }


    internal void SetupConfig() {

        // auto load
        Config.Bind(ConfigAliasSection, "Enable Autoload", false, "Allow this mod to automatically load your alias definition upon game start");
        Config.Bind(ConfigAliasSection, "Autoload File", "aliasAutoLoad", "The target file name to load from");

        // fast ConfigTerminalSection
        Config.Bind(ConfigTerminalSection, "Skip Bootup Text", true, "Skip terminal bootup text");

        //dev section
        Config.Bind("Debug", "Debug Mode", false, "Turn on debug mode for mods developer");

        Config.Save();
    }
}
