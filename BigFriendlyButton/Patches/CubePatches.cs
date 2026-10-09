using BigFriendlyButton.Services;
using HarmonyLib;

namespace BigFriendlyButton.Patches
{
    // the cosmetic box that stands in for the shop button on screens without the mod
    [HarmonyPatch]
    internal static class CubePatches
    {
        [HarmonyPatch(typeof(CosmeticWorldObject), nameof(CosmeticWorldObject.Start))]
        [HarmonyPostfix]
        private static void Spawned(CosmeticWorldObject __instance) => ExtractionButton.Adopt(__instance);

        // its health is the haul bar now, so holding the button must not heal it off your own health
        [HarmonyPatch(typeof(CosmeticWorldObject), nameof(CosmeticWorldObject.HealthLogic))]
        [HarmonyPrefix]
        private static void NoHealing(CosmeticWorldObject __instance)
        {
            if (ExtractionButton.For(__instance.physGrabObject) != null)
                __instance.healTimer = 0f;
        }

        // singleplayer adds the grabber straight from GrabStarted, no rpc
        [HarmonyPatch(typeof(PhysGrabObject), nameof(PhysGrabObject.GrabStarted))]
        [HarmonyPrefix]
        private static void BeforeSoloGrab(PhysGrabObject __instance, out int __state) => __state = __instance.playerGrabbing.Count;

        [HarmonyPatch(typeof(PhysGrabObject), nameof(PhysGrabObject.GrabStarted))]
        [HarmonyPostfix]
        private static void AfterSoloGrab(PhysGrabObject __instance, int __state)
        {
            if (!SemiFunc.IsMultiplayer())
                PressIfNew(__instance, __state);
        }

        [HarmonyPatch(typeof(PhysGrabObject), nameof(PhysGrabObject.GrabStartedRPC))]
        [HarmonyPrefix]
        private static void BeforeGrab(PhysGrabObject __instance, out int __state) => __state = __instance.playerGrabbing.Count;

        [HarmonyPatch(typeof(PhysGrabObject), nameof(PhysGrabObject.GrabStartedRPC))]
        [HarmonyPostfix]
        private static void AfterGrab(PhysGrabObject __instance, int __state)
        {
            if (SemiFunc.IsMasterClient())
                PressIfNew(__instance, __state);
        }

        private static void PressIfNew(PhysGrabObject grab, int countBefore)
        {
            if (grab.playerGrabbing.Count <= countBefore)
                return;

            ExtractionButton? button = ExtractionButton.For(grab);
            PhysGrabber grabber = grab.playerGrabbing[grab.playerGrabbing.Count - 1];
            if (button != null && grabber)
                button.OnPress(grabber, fromCube: true);
        }
    }
}
