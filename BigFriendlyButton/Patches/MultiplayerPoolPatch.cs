using BigFriendlyButton.Services;
using HarmonyLib;
using UnityEngine;

namespace BigFriendlyButton.Patches
{
    // a host running Zehs button spawns it on us too. we dont have his prefab, so build our stand in instead
    [HarmonyPatch(typeof(MultiplayerPool), nameof(MultiplayerPool.Instantiate))]
    internal static class MultiplayerPoolPatch
    {
        private static bool Prefix(MultiplayerPool __instance, string prefabId, Vector3 position, Quaternion rotation, ref GameObject? __result)
        {
            if (__instance.ResourceCache.ContainsKey(prefabId) || !ZehsStandIn.IsZehsButton(prefabId))
                return true;

            __result = ZehsStandIn.Build(position, rotation);
            // couldnt build one, let the game fail the way it would have
            return __result == null;
        }
    }
}
