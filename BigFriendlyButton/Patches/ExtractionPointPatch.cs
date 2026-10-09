using BigFriendlyButton.Services;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace BigFriendlyButton.Patches
{
    [HarmonyPatch(typeof(ExtractionPoint))]
    internal static class ExtractionPointPatch
    {
        // Start deletes the shop stand outside the shop. hand it a stand in to delete and keep the real one.
        // the field stays pointed at the deleted stand in so the games own !shopStation checks still go the vanilla way
        [HarmonyPatch(nameof(ExtractionPoint.Start))]
        [HarmonyPrefix]
        private static void KeepShopStation(ExtractionPoint __instance, out Transform? __state)
        {
            __state = null;
            if (!SemiFunc.RunIsLevel() || !Gate.Present || !__instance.shopStation)
                return;

            __state = __instance.shopStation;
            var standIn = new GameObject("Shop Station");
            standIn.transform.SetParent(__instance.transform, false);
            __instance.shopStation = standIn.transform;
        }

        [HarmonyPatch(nameof(ExtractionPoint.Start))]
        [HarmonyPostfix]
        private static void Placed(ExtractionPoint __instance, Transform? __state)
        {
            if (__state != null)
                ExtractionButton.Attach(__instance, __state);
            Presence.Publish();
        }

        // shop buttons own click is the pay logic, skip it on ours
        [HarmonyPatch(nameof(ExtractionPoint.OnShopClick))]
        [HarmonyPrefix]
        private static bool SkipShopClick(ExtractionPoint __instance) => !__instance.TryGetComponent(out ExtractionButton _);

        [HarmonyPatch(nameof(ExtractionPoint.ShopButtonAnimation))]
        [HarmonyPostfix]
        private static void ShopButtonAnimated(ExtractionPoint __instance)
        {
            if (__instance.TryGetComponent(out ExtractionButton button))
                button.OnShopAnimationTick();
        }

        [HarmonyPatch(nameof(ExtractionPoint.HaulChecker))]
        [HarmonyPostfix]
        private static void HaulChecked(ExtractionPoint __instance)
        {
            if (__instance.TryGetComponent(out ExtractionButton button))
                button.OnHaulChecked();
        }

        [HarmonyPatch(nameof(ExtractionPoint.StateSetRPC))]
        [HarmonyPostfix]
        private static void StateChanged(ExtractionPoint __instance, ExtractionPoint.State state, PhotonMessageInfo _info)
        {
            if (!SemiFunc.MasterOnlyRPC(_info))
                return;
            if (__instance.TryGetComponent(out ExtractionButton button))
                button.OnStateSet(state);
            ZehsStandIn.OnStateSet(__instance, state);
        }

        // goal changed mid Active (early send canceled) and the game already drew the bar off the old one
        [HarmonyPatch(nameof(ExtractionPoint.HaulGoalSetRPC))]
        [HarmonyPostfix]
        private static void GoalChanged(ExtractionPoint __instance, PhotonMessageInfo _info)
        {
            if (SemiFunc.MasterOnlyRPC(_info) && __instance.currentState == ExtractionPoint.State.Active && __instance.TryGetComponent(out ExtractionButton _))
                __instance.SetHaulText();
        }

        // game asks for Success every frame once the goal is met, hold it till somebody presses
        [HarmonyPatch(nameof(ExtractionPoint.StateSet))]
        [HarmonyPrefix]
        private static bool HoldSuccess(ExtractionPoint __instance, ExtractionPoint.State newState)
        {
            if (newState != ExtractionPoint.State.Success || __instance.currentState != ExtractionPoint.State.Active)
                return true;

            return !__instance.TryGetComponent(out ExtractionButton button) || !button.HoldsSuccess;
        }

        // fresh point opening picks up whatever early sends owe
        [HarmonyPatch(nameof(ExtractionPoint.HaulGoalSet))]
        [HarmonyPrefix]
        private static void AddShortfall(ExtractionPoint __instance, ref int value)
        {
            if (SemiFunc.IsMasterClientOrSingleplayer()
                && __instance.TryGetComponent(out ExtractionButton button)
                && !button.SettingOwnGoal)
            {
                value += Shortfall.Take();
            }
        }
    }
}
