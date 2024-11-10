using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTerminalEX.Patches {
    [HarmonyPatch(typeof(TerminalManager), "Awake")]
    internal static class TerminalManagerAwakePatches {

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        internal static void Postfix(ref TerminalManager __instance) {
            __instance.skipBoot = true;
        }
    }
}
