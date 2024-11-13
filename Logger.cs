using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTerminalEX {
    internal static class PLog {
        private static ManualLogSource? _log;
        private static bool _init = false;

        public static void InitLog(ManualLogSource logSource) {
            if (_init) return;

            _log = logSource;
            _init = true;
        }

        public static void Inf(string msg) {
            _log?.LogInfo(msg);
        }

        public static void Err(string msg) {
            _log?.LogError(msg);
        }

        public static void Dbg(string msg) {
            if (Plugin.pluginInstance.DebugMode)
                _log?.LogInfo(msg);
        }

        public static void Wrn(string msg) {
            _log?.LogWarning(msg);
        }
    }
}
