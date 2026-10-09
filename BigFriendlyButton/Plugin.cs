using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BigFriendlyButton
{
    [BepInPlugin(Guid, "BigFriendlyButton", BuildInfo.Version)]
    public class BigFriendlyButtonPlugin : BaseUnityPlugin
    {
        internal const string Guid = "Vippy.BigFriendlyButton";

        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;
            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            PressConfig.Init(Config);
            new Harmony(Guid).PatchAll();

            Logger.LogInfo($"BigFriendlyButton v{BuildInfo.Version} loaded");
        }
    }
}
