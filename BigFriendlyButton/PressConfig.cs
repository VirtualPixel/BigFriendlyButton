using BepInEx.Configuration;
using BigFriendlyButton.Services;

namespace BigFriendlyButton
{
    internal static class PressConfig
    {
        public static ConfigEntry<bool> Enabled = null!;
        public static ConfigEntry<bool> HostOnly = null!;
        public static ConfigEntry<bool> PressToCancel = null!;
        public static ConfigEntry<bool> SendEarly = null!;
        public static ConfigEntry<int> AutoExtractAfter = null!;
        public static ConfigEntry<bool> PreviewNoModButton = null!;
        public static ConfigEntry<bool> ProgressBar = null!;

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind("Button", "Enabled", true,
                "Turn it off and extraction goes back to vanilla on the spot, the haul goes the second you hit the goal. " +
                "Joining someone with Zehs button still works either way.");

            HostOnly = config.Bind("Button", "Only host can press", false,
                "Only the host can send the haul or call it off. Everyone elses button stays dark. " +
                "Turns itself off while the host is dead so the haul can't get stuck.");

            PressToCancel = config.Bind("Button", "Press again to cancel", true,
                "Hit the button again before the extraction goes to call it off. " +
                "Nothing on the pad goes anywhere.");

            SendEarly = config.Bind("Button", "Send early", false,
                "Lets a press send the haul before it reaches the goal. Whatever it was short gets added to the " +
                "next extraction point's goal. On the last point of a level its just gone.");

            AutoExtractAfter = config.Bind("Button", "Auto extract after", 0, new ConfigDescription(
                "Seconds at the goal before it goes on its own. 0 = never, it waits for a press.",
                new AcceptableValueRange<int>(0, 300)));

            ProgressBar = config.Bind("Button", "Progress bar", true,
                "A bar on the stand that fills as you get closer to being able to press. Just your screen. " +
                "With Enabled off it moves to the extraction screen and shows the haul against the goal.");

            PreviewNoModButton = config.Bind("Button", "Preview the no-mod button", false,
                "Host only. Shows you what people without the mod get (the flat box) instead of the shop button, so you can try it solo.");

            Enabled.SettingChanged += (_, _) => ExtractionButton.SettingsChanged();
            SendEarly.SettingChanged += (_, _) => Presence.Publish();
            HostOnly.SettingChanged += (_, _) => Presence.Publish();
            PressToCancel.SettingChanged += (_, _) => Presence.Publish();
        }
    }
}
