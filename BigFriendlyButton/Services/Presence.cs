using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace BigFriendlyButton.Services
{
    // player tag so the host knows who can see the shop button (no cube needed for them) and modded poeple can
    // light their button off whatever the host has set
    internal static class Presence
    {
        private const string Key = "Vippy.BigFriendlyButton";

        private const int Off = 0;
        private const int Live = 1;
        private const int SendEarly = 2;
        private const int HostOnly = 4;
        private const int Cancel = 8;

        private static Room? sentRoom;
        private static int sentValue;

        public static void Publish()
        {
            if (!PhotonNetwork.InRoom)
                return;

            int value = Local();
            if (sentRoom == PhotonNetwork.CurrentRoom && sentValue == value)
                return;

            sentRoom = PhotonNetwork.CurrentRoom;
            sentValue = value;
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { Key, value } });
        }

        // has the mod AND its on, so they can see the shop button
        public static bool HasMod(Player player) => player.CustomProperties.TryGetValue(Key, out object value) && value is int tag && (tag & Live) != 0;

        public static bool HostLive() => (Host() & Live) != 0;

        public static bool HostSendsEarly() => (Host() & SendEarly) != 0;

        public static bool HostOnlyPresses() => (Host() & HostOnly) != 0;

        public static bool HostCancels() => (Host() & Cancel) != 0;

        // only modded poeple have the stand
        public static void SendToModded(PhotonView view, string rpc, bool includeSelf, object[] args)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal ? includeSelf : player.IsMasterClient || HasMod(player))
                    view.RPC(rpc, player, args);
            }
        }

        // only what this level actually has. turning it on mid level doesnt give anyone a shop stand
        private static int Local()
        {
            if (!Gate.Live || !ExtractionButton.AnyPlaced)
                return Off;
            return Live
                | (PressConfig.SendEarly.Value ? SendEarly : 0)
                | (PressConfig.HostOnly.Value ? HostOnly : 0)
                | (PressConfig.PressToCancel.Value ? Cancel : 0);
        }

        private static int Host()
        {
            if (SemiFunc.IsMasterClientOrSingleplayer())
                return Local();

            Player? host = PhotonNetwork.MasterClient;
            return host != null && host.CustomProperties.TryGetValue(Key, out object value) && value is int tag ? tag : Off;
        }
    }
}
