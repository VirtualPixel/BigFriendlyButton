using BigFriendlyButton.Services;
using HarmonyLib;
using Photon.Pun;

namespace BigFriendlyButton.Patches
{
    // shop button presses. press = new grabber on the button. mp grabs all land on the host as GrabStartedRPC,
    // sp goes through GrabStarted. count check so only grabs the game actualy took count, holding = one press
    [HarmonyPatch(typeof(StaticGrabObject))]
    internal static class StaticGrabObjectPatch
    {
        // a client whose copy got switched off mid grab never sends GrabEnded, drop the leftover so this grab counts
        [HarmonyPatch(nameof(StaticGrabObject.GrabStartedRPC))]
        [HarmonyPrefix]
        private static void BeforeNetworkGrab(StaticGrabObject __instance, int playerPhotonID, out int __state)
        {
            if (ExtractionButton.For(__instance) != null)
            {
                PhotonView view = PhotonView.Find(playerPhotonID);
                if (view && view.TryGetComponent(out PhysGrabber stale))
                    __instance.playerGrabbing.Remove(stale);
            }
            __state = __instance.playerGrabbing.Count;
        }

        [HarmonyPatch(nameof(StaticGrabObject.GrabStartedRPC))]
        [HarmonyPostfix]
        private static void AfterNetworkGrab(StaticGrabObject __instance, int __state)
        {
            if (SemiFunc.IsMasterClient())
                PressIfNew(__instance, __state);
        }

        [HarmonyPatch(nameof(StaticGrabObject.GrabStarted))]
        [HarmonyPrefix]
        private static void BeforeLocalGrab(StaticGrabObject __instance, out int __state) => __state = __instance.playerGrabbing.Count;

        [HarmonyPatch(nameof(StaticGrabObject.GrabStarted))]
        [HarmonyPostfix]
        private static void AfterLocalGrab(StaticGrabObject __instance, int __state)
        {
            if (!SemiFunc.IsMultiplayer())
                PressIfNew(__instance, __state);
        }

        // people without the mod deleted their copy of the shop button, dont send them its grab visuals
        // (theyd just log a missing view every grab)
        [HarmonyPatch(nameof(StaticGrabObject.GrabLink))]
        [HarmonyPrefix]
        private static bool LinkModdedOnly(StaticGrabObject __instance, object[] __args)
        {
            if (!SemiFunc.IsMultiplayer() || ExtractionButton.For(__instance) == null)
                return true;

            Presence.SendToModded(__instance.photonView, nameof(StaticGrabObject.GrabLinkRPC), true, __args);
            return false;
        }

        // same thing for the ownership hand off on Start, the host already owns room objects anyway
        [HarmonyPatch(nameof(StaticGrabObject.Start))]
        [HarmonyPrefix]
        private static bool SkipShopOwnership(StaticGrabObject __instance)
        {
            if (SemiFunc.RunIsShop())
                return true;
            ExtractionPoint point = __instance.GetComponentInParent<ExtractionPoint>();
            if (!point || point.buttonGrabObject == __instance)
                return true;

            __instance.photonView = __instance.GetComponent<PhotonView>();
            return false;
        }

        // same for the grab area telling everyone whos holding it
        [HarmonyPatch(typeof(PhysGrabObjectGrabArea), nameof(PhysGrabObjectGrabArea.UpdateList))]
        [HarmonyPrefix]
        private static bool AreaModdedOnly(PhysGrabObjectGrabArea __instance, bool add, PhysGrabber grabber)
        {
            if (!SemiFunc.IsMultiplayer() || !grabber || !__instance.staticGrabObject || ExtractionButton.For(__instance.staticGrabObject) == null)
                return true;

            string rpc = add ? nameof(PhysGrabObjectGrabArea.AddToGrabbersList) : nameof(PhysGrabObjectGrabArea.RemoveFromGrabbersList);
            Presence.SendToModded(__instance.photonView, rpc, false, new object[] { grabber.photonView.ViewID });
            return false;
        }

        private static void PressIfNew(StaticGrabObject grab, int countBefore)
        {
            if (grab.playerGrabbing.Count <= countBefore)
                return;

            ExtractionButton? button = ExtractionButton.For(grab);
            PhysGrabber grabber = grab.playerGrabbing[grab.playerGrabbing.Count - 1];
            if (button != null && grabber)
                button.OnPress(grabber);
        }
    }
}
