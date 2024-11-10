using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTerminalEX.Patches {

    [HarmonyPatch(typeof(NetworkManager), "OnJoinedRoom")]
    internal class NetworkManagerOnJoinedRoomPatches {

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        internal static void Postfix() {
            Plugin.instance.OnGameStartup();
        }
    }
}
