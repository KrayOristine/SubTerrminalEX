
using SubTerminalEX.Features;

namespace SubTerminalEX;

[BepInPlugin(SubterexInfo.PLUG_GUID, SubterexInfo.PLUG_NAME, SubterexInfo.PLUG_VER)]
public class Plugin : BaseUnityPlugin
{
    internal static Harmony _harmony;
    internal static Plugin instance;

    private void Awake()
    {
        instance = this;

        if (_harmony != null) {
            _harmony.UnpatchSelf();
        }

        // Plugin startup logic
        PLog.InitLog(base.Logger);

        PLog.Inf($"Loaded successfully");

        // fuck around
        _harmony = new Harmony(SubterexInfo.PLUG_GUID);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());

        PLog.Inf("Doing startup chores...");

        Startup();

        PLog.Inf("Started up!");
    }

    private static void OnDestroy() {
        if (_harmony == null) return;

        _harmony.UnpatchSelf();
    }

    private void Startup() {
        ExtraTerminalCommand.Register();
    }

    internal void OnGameStartup() {
        PLog.Inf("Doing late startup...");
        TerminalCommandManager.AddToGame();
    }
}
