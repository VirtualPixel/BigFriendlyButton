namespace BigFriendlyButton.Services
{
    // Leaving is the 3-2-1
    internal enum Phase
    {
        Filling,
        Leaving,
        Settling,
        Done,
    }

    internal enum PressResult
    {
        Ignore,
        Deny,
        Confirm,
        SendEarly,
        Cancel,
    }

    internal readonly struct PressFacts
    {
        public readonly bool GoalMet;
        public readonly bool ByHost;
        public readonly float SecondsLeaving;

        public PressFacts(bool goalMet, bool byHost, float secondsLeaving)
        {
            GoalMet = goalMet;
            ByHost = byHost;
            SecondsLeaving = secondsLeaving;
        }
    }

    internal readonly struct PressSettings
    {
        public readonly bool HostOnly;
        public readonly bool CanCancel;
        public readonly bool CanSendEarly;

        public PressSettings(bool hostOnly, bool canCancel, bool canSendEarly)
        {
            HostOnly = hostOnly;
            CanCancel = canCancel;
            CanSendEarly = canSendEarly;
        }
    }

    internal static class PressRules
    {
        // 2 people hitting it at once land a bit apart on the host, without this the second one cancels the first
        public const float CancelGrace = 1.25f;

        // owed off the goal we actually set. anything thrown in after the send already comes back as tax return
        public static int Owed(int fullGoal, int sentGoal) => fullGoal > sentGoal ? fullGoal - sentGoal : 0;

        public static PressResult Decide(Phase phase, PressFacts facts, PressSettings settings)
        {
            if (phase == Phase.Settling || phase == Phase.Done)
                return PressResult.Ignore;
            if (phase == Phase.Leaving && facts.SecondsLeaving < CancelGrace)
                return PressResult.Ignore;

            if (settings.HostOnly && !facts.ByHost)
                return PressResult.Deny;

            if (phase == Phase.Leaving)
                return settings.CanCancel ? PressResult.Cancel : PressResult.Deny;
            if (facts.GoalMet)
                return PressResult.Confirm;
            // even with nothing on the pad, the game is fine with a $0 goal (bar, emoji and payout all handle it)
            if (settings.CanSendEarly)
                return PressResult.SendEarly;
            return PressResult.Deny;
        }
    }
}
