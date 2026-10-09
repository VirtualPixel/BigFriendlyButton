using BepInEx.Bootstrap;

namespace BigFriendlyButton.Services
{
    internal static class Gate
    {
        public const string ZehsGuid = "com.github.zehsteam.ExtractionPointConfirmButton";

        // these already add a confirm step, two gates = two presses to leave so we just back off
        private static readonly string[] OtherConfirmMods =
        {
            ZehsGuid,
            "DJDarkLucky.AddExtractionButton",
            "com.repo.extractionconfirm",
            "yourname.extractionconfirm", // Manual_Extraction_Confirm, they never changed the template guid
            "zichen.extractiondelayconfirm",
            "wzc.repo.auto_extraction_confirm",
        };

        private static bool checkedOthers;
        private static bool otherPresent;

        // no other confirm mod here, so the stands get kept and the button can be switched on and off live
        public static bool Present => !OtherConfirmModLoaded();

        public static bool Live => PressConfig.Enabled.Value && Present;

        public static bool ZehsLoaded => Chainloader.PluginInfos.ContainsKey(ZehsGuid);

        private static bool OtherConfirmModLoaded()
        {
            if (checkedOthers)
                return otherPresent;

            checkedOthers = true;
            foreach (string guid in OtherConfirmMods)
            {
                if (!Chainloader.PluginInfos.ContainsKey(guid))
                    continue;

                otherPresent = true;
                BigFriendlyButtonPlugin.Log.LogWarning($"{guid} already adds a confirm button, BigFriendlyButton is turning itself off");
                break;
            }
            return otherPresent;
        }
    }
}
