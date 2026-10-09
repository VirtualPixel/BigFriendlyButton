using BigFriendlyButton.Services;
using Xunit;

namespace BigFriendlyButton.Tests
{
    public class PressRulesTests
    {
        private const float WellIntoCountdown = PressRules.CancelGrace + 1f;

        [Fact]
        public void PressAtTheGoalSendsIt() =>
            Assert.Equal(PressResult.Confirm, Decide(Phase.Filling, goalMet: true));

        [Fact]
        public void PressShortOfTheGoalIsANope() =>
            Assert.Equal(PressResult.Deny, Decide(Phase.Filling));

        [Fact]
        public void PressDuringTheCountdownCancels() =>
            Assert.Equal(PressResult.Cancel, Decide(Phase.Leaving, goalMet: true, secondsLeaving: WellIntoCountdown));

        [Fact]
        public void SecondPlayerRightBehindTheFirstDoesNotCancel() =>
            Assert.Equal(PressResult.Ignore, Decide(Phase.Leaving, goalMet: true, secondsLeaving: 0.3f));

        [Fact]
        public void CancelOffMakesTheCountdownPressANope() =>
            Assert.Equal(PressResult.Deny, Decide(Phase.Leaving, goalMet: true, secondsLeaving: WellIntoCountdown, canCancel: false));

        [Fact]
        public void PressWhileTheRedXIsUpIsIgnored() =>
            Assert.Equal(PressResult.Ignore, Decide(Phase.Settling, goalMet: true));

        [Fact]
        public void PressOnAFinishedPointIsQuiet() =>
            Assert.Equal(PressResult.Ignore, Decide(Phase.Done, goalMet: true));

        [Fact]
        public void HostOnlyNopesEveryoneElse()
        {
            Assert.Equal(PressResult.Deny, Decide(Phase.Filling, goalMet: true, byHost: false, hostOnly: true));
            Assert.Equal(PressResult.Deny, Decide(Phase.Leaving, goalMet: true, byHost: false, hostOnly: true, secondsLeaving: WellIntoCountdown));
        }

        [Fact]
        public void HostOnlyStillLetsTheHostThrough()
        {
            Assert.Equal(PressResult.Confirm, Decide(Phase.Filling, goalMet: true, hostOnly: true));
            Assert.Equal(PressResult.Cancel, Decide(Phase.Leaving, goalMet: true, hostOnly: true, secondsLeaving: WellIntoCountdown));
        }

        [Fact]
        public void AnyoneCanPressWhenHostOnlyIsOff() =>
            Assert.Equal(PressResult.Confirm, Decide(Phase.Filling, goalMet: true, byHost: false));

        [Fact]
        public void SendEarlyLetsAShortHaulGo() =>
            Assert.Equal(PressResult.SendEarly, Decide(Phase.Filling, canSendEarly: true));

        [Fact]
        public void AtTheGoalItsANormalSendEvenWithSendEarlyOn() =>
            Assert.Equal(PressResult.Confirm, Decide(Phase.Filling, goalMet: true, canSendEarly: true));

        // owed comes off the cut goal. loot thrown in during the 3-2-1 comes back as tax return anyway
        [Fact]
        public void EarlySendOwesWhatTheGoalWasCut() =>
            Assert.Equal(12000, PressRules.Owed(fullGoal: 20000, sentGoal: 8000));

        [Fact]
        public void NothingOwedWhenTheGoalWasNeverCut() =>
            Assert.Equal(0, PressRules.Owed(fullGoal: 20000, sentGoal: 20000));

        private static PressResult Decide(
            Phase phase,
            bool goalMet = false,
            bool byHost = true,
            float secondsLeaving = 0f,
            bool hostOnly = false,
            bool canCancel = true,
            bool canSendEarly = false) =>
            PressRules.Decide(phase,
                new PressFacts(goalMet, byHost, secondsLeaving),
                new PressSettings(hostOnly, canCancel, canSendEarly));
    }
}
