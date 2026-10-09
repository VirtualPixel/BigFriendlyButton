using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace BigFriendlyButton.Services
{
    // one per extraction point. people with the mod get the shop's own button on its stand next to the pad,
    // people without it get the smallest cosmetic box pinned in the same spot, its got a real button on it.
    // the host reads presses on both
    internal sealed class ExtractionButton : MonoBehaviour
    {
        // same orange as the games READY + the shop buttons glow
        internal static readonly Color ReadyColor = new(1f, 0.5f, 0f);

        internal static readonly Vector3 StandScale = new(0.7f, 0.7f, 0.7f);
        internal static readonly Vector3 StandOffset = new(0.065f, 0f, 0.402f);

        private const string CubeTag = "Vippy.BigFriendlyButton";

        // flat so its just the button face, but not paper thin, it pushes in along z. the game applies the scale on every machine (PhysGrabObject.Awake)
        private static readonly Vector3 CubeScale = new(1f, 1f, 0.15f);

        private static readonly List<ExtractionButton> Placed = new();

        // singleplayer boxes come from a plain Instantiate, no spawn data to tag them with
        private static readonly Dictionary<GameObject, ExtractionButton> SoloCubes = new();

        private ExtractionPoint point = null!;
        private GameObject station = null!;
        private StaticGrabObject shopGrab = null!;
        private Light shopLight = null!;
        private MeshRenderer shopVisual = null!;
        private GameObject? cubeObject;
        private PhysGrabObject? cube;
        private NotValuableObject? cubeHealth;
        private ProgressBar? standBar;
        private ProgressBar? backBar;
        private bool barsTried;
        private bool? lit;
        private bool wantLit;
        private bool squishing;
        private bool? cubeShown;
        private readonly List<Renderer> hiddenRenderers = new();
        private readonly List<Collider> hiddenColliders = new();
        private readonly List<Light> hiddenLights = new();
        private float nextCubeCheck;
        private bool screenReady;
        private bool confirming;
        private bool settingGoal;
        private int? fullGoal;
        private float sentAt;
        private float goalMetFor;
        private string? readyText;
        private string? activeText;

        // real stand from the Start prefix
        public static void Attach(ExtractionPoint point, Transform station)
        {
            ExtractionButton button = point.gameObject.AddComponent<ExtractionButton>();
            button.point = point;
            button.station = station.gameObject;
            button.shopGrab = station.GetComponentInChildren<StaticGrabObject>(true);
            button.shopLight = button.shopGrab.GetComponentInChildren<Light>(true);
            button.shopVisual = point.shopButton.GetComponent<MeshRenderer>();

            // the shops spot is out to the side and ends up in the wall in a lot of rooms. pull the whole stand in
            // to where Zehs puts his (pedestal at 1.99, 0, 1.13 at 0.7 size) so it sits in front of the pad
            station.localScale = StandScale;
            station.localPosition = StandOffset;
            button.station.SetActive(false);
            Placed.Add(button);
        }

        // a shop stand to copy. ours once a point has started, otherwise the games own before Start deletes it
        public static Transform? AnyShopStation()
        {
            if (Placed.Count > 0)
                return Placed[0].station.transform;

            foreach (ExtractionPoint point in FindObjectsOfType<ExtractionPoint>())
            {
                if (point.shopStation && point.shopStation.childCount > 0)
                    return point.shopStation;
            }
            return null;
        }

        public static ExtractionButton? For(StaticGrabObject grab)
        {
            foreach (ExtractionButton button in Placed)
            {
                if (button.shopGrab == grab)
                    return button;
            }
            return null;
        }

        public static ExtractionButton? For(PhysGrabObject grab)
        {
            foreach (ExtractionButton button in Placed)
            {
                if (button.cube == grab)
                    return button;
            }
            return null;
        }

        // runs on every machine when a cube the host spawned shows up. pull it out of the games cosmetic box list
        // straight away so the game never treats it like loot
        public static void Adopt(CosmeticWorldObject cube)
        {
            bool solo = SoloCubes.TryGetValue(cube.gameObject, out ExtractionButton? owner);
            if (solo)
                SoloCubes.Remove(cube.gameObject);
            else if (!IsCube(cube.GetComponent<PhotonView>()))
                return;

            RoundDirector.instance.cosmeticWorldObjects.Remove(cube);
            cube.tutorialActive = false;

            if (cube.TryGetComponent(out ValuableDiscoverCustom discover))
            {
                discover.discovered = true; // no discover flash, and item trackers leave it alone
                if (Gate.Present)
                    discover.customMap.Clear(); // no cosmetic box icon on the map for a box this screen hides
            }

            owner ??= OwnerOf(cube.GetComponent<PhotonView>().InstantiationData[4]);
            owner?.TakeCube(cube);
        }

        private static ExtractionButton? OwnerOf(object pointView)
        {
            foreach (ExtractionButton button in Placed)
            {
                if (pointView is int view && button.point.photonView.ViewID == view)
                    return button;
            }
            return null;
        }

        public static bool AnyPlaced => Placed.Count > 0;

        // only on the hosts screen, thats the one that spawns the box. a friend left with it on would get no button at all
        private static bool Previewing => PressConfig.PreviewNoModButton.Value && SemiFunc.IsMasterClientOrSingleplayer();

        // Enabled flipped off, dont wait for the next Active frame (the countdown doesnt run HaulChecker)
        public static void SettingsChanged()
        {
            Presence.Publish();
            if (Gate.Live)
                return;
            foreach (ExtractionButton button in Placed)
                button.HandBack();
        }

        public static bool IsCube(PhotonView view) =>
            view && view.InstantiationData is { Length: 5 } data && data[3] is string tag && tag == CubeTag;

        public bool HoldsSuccess => !confirming && SemiFunc.IsMasterClientOrSingleplayer() && Gate.Live;

        public bool SettingOwnGoal => settingGoal;

        private bool GoalMet => point.haulGoalFetched && point.haulGoal - point.haulCurrent <= 0;

        private static bool IsFinished(ExtractionPoint.State state) =>
            state is ExtractionPoint.State.Extracting or ExtractionPoint.State.TaxReturn or ExtractionPoint.State.Complete;

        private void OnDestroy() => Placed.Remove(this);

        // every frame in Active, every machine
        public void OnHaulChecked()
        {
            bool host = SemiFunc.IsMasterClientOrSingleplayer();
            bool leaving = point.settingState || point.stateSetTo != ExtractionPoint.State.None;
            if (host && !leaving)
                RestoreGoal();

            if (!Presence.HostLive() || !Gate.Live)
            {
                HandBack();
                // button off, the bar at the back shows the haul against the goal before vanilla sends it
                ShowBars(HaulProgress, onStand: false);
                return;
            }

            // preview: see it the way someone without the mod does, box instead of the shop stand
            bool preview = Previewing;
            station.SetActive(!preview);
            ShowCube(preview);

            bool canPress = ICanPress();

            // on the way out the button stays live only if a press would cancel
            if (leaving)
            {
                ShowReady(false);
                bool canCancel = canPress && Presence.HostCancels();
                SetPressable(canCancel);
                SetLit(canCancel);
                return;
            }

            bool ready = GoalMet;
            bool sendable = Presence.HostSendsEarly();
            // lit whenever a press from here does something. READY on the screen still means the goal is met
            SetPressable(canPress && (ready || sendable));
            SetLit(canPress && (ready || sendable));
            ShowReady(ready);
            ShowBars(PressProgress(canPress, sendable), onStand: true);

            if (!host)
                return;
            EnsureCube();
            // the box is for people without the mod so its bar goes off their press
            bool othersCanPress = !PressConfig.HostOnly.Value || HostIsDown() || !SemiFunc.IsMultiplayer();
            ShowProgressOnCube(PressProgress(othersCanPress, PressConfig.SendEarly.Value));
            TickAutoExtract(ready);
        }

        public void OnStateSet(ExtractionPoint.State state)
        {
            // squish on a send, and on a cancel from the countdown with the haul still there (thats a press, the
            // game cancelling on its own means the haul dropped)
            bool fromCountdown = point.currentState is ExtractionPoint.State.Success or ExtractionPoint.State.Surplus or ExtractionPoint.State.Warning;
            if (state == ExtractionPoint.State.Success || (state == ExtractionPoint.State.Cancel && fromCountdown && GoalMet))
                PlayPress();

            if (!IsFinished(state))
                return;

            ShowReady(false);
            point.shopButtonAnimation = false;
            point.shopButton.localScale = Vector3.one;
            squishing = false;
            SetLit(false);
            SetPressable(false);

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;
            if (state == ExtractionPoint.State.Extracting)
                CommitShortfall();
            if (state == ExtractionPoint.State.Complete)
                DropCube();
        }

        // cube is pinned so its just a button, one grab one press
        public void OnPress(PhysGrabber grabber, bool fromCube = false)
        {
            Phase? phase = PhaseOf(point.currentState);
            if (phase == null || !Gate.Live)
                return;

            var facts = new PressFacts(GoalMet, grabber.isLocal, Time.time - sentAt);
            PressResult result = PressRules.Decide(phase.Value, facts, Settings());
            BigFriendlyButtonPlugin.Log.LogDebug($"[Press] {(fromCube ? "cube" : "button")} {point.currentState}: {result}");

            switch (result)
            {
                case PressResult.Confirm:
                    Confirm();
                    break;
                case PressResult.SendEarly:
                    SendEarly();
                    break;
                case PressResult.Cancel:
                    Cancel();
                    break;
                case PressResult.Deny:
                    Deny();
                    break;
            }
        }

        private void PlayPress()
        {
            if (!station.activeSelf)
                return;
            point.shopButton.localScale = new Vector3(1f, 0.1f, 1f);
            point.soundButton.Play(point.shopButton.position);
            point.shopButtonAnimationEval = 0f;
            point.shopButtonAnimation = true;
            squishing = true;
        }

        // the squish ends on the shops glow, put back whatever the button should look like now
        public void OnShopAnimationTick()
        {
            if (!squishing || point.shopButtonAnimation)
                return;
            squishing = false;
            lit = null;
            ApplyLit();
        }

        // only the host can press (unless theyre dead, then anyone can), mirrored off the hosts settings
        private static bool ICanPress()
        {
            if (SemiFunc.IsMasterClientOrSingleplayer() || !Presence.HostOnlyPresses())
                return true;

            foreach (PlayerAvatar avatar in SemiFunc.PlayerGetAll())
            {
                if (avatar && avatar.photonView && avatar.photonView.Owner != null && avatar.photonView.Owner.IsMasterClient)
                    return avatar.isDisabled;
            }
            return true; // no host avatar counts as down, same as the host sees it
        }

        private static PressSettings Settings()
        {
            // dead host cant press, dont let that setting lock the haul in
            return new PressSettings(
                PressConfig.HostOnly.Value && !HostIsDown(),
                PressConfig.PressToCancel.Value,
                PressConfig.SendEarly.Value);
        }

        private static Phase? PhaseOf(ExtractionPoint.State state) => state switch
        {
            ExtractionPoint.State.Active => Phase.Filling,
            ExtractionPoint.State.Success or ExtractionPoint.State.Surplus or ExtractionPoint.State.Warning => Phase.Leaving,
            ExtractionPoint.State.Cancel => Phase.Settling,
            ExtractionPoint.State.Extracting or ExtractionPoint.State.TaxReturn or ExtractionPoint.State.Complete => Phase.Done,
            _ => null,
        };

        private void SetPressable(bool on)
        {
            if (shopGrab.enabled != on)
                shopGrab.enabled = on;
        }

        // turned off mid point (host or just us), back to vanilla on the spot
        private void HandBack()
        {
            ShowReady(false);
            station.SetActive(false);
            ShowCube(true);
            if (SemiFunc.IsMasterClientOrSingleplayer())
                DropCube();
        }

        // only spawned when someone in the lobby can't see the shop button
        private void EnsureCube()
        {
            if (Time.time < nextCubeCheck)
                return;
            nextCubeCheck = Time.time + 1f;
            // the preview box goes with the preview, unless someone in the lobby still needs one
            if (cubeObject && !Previewing && (!SemiFunc.IsMultiplayer() || !AnyoneWithoutTheMod()))
                DropCube();
            if (cubeObject)
                return;
            bool solo = !SemiFunc.IsMultiplayer();
            if (!Previewing && (solo || !AnyoneWithoutTheMod()))
                return;

            PrefabRef? prefab = SmallestCube();
            if (prefab == null)
                return;

            // solo is only ever the preview, spawned the way the game spawns things in singleplayer
            if (solo)
            {
                cubeObject = Instantiate(prefab.Prefab, CubeSpot, CubeFacing);
                cubeObject.transform.localScale = CubeScale;
                SoloCubes[cubeObject] = this;
                return;
            }

            cubeObject = PhotonNetwork.InstantiateRoomObject(prefab.ResourcePath, CubeSpot, CubeFacing, 0,
                new object[] { CubeScale.x, CubeScale.y, CubeScale.z, CubeTag, point.photonView.ViewID });
        }

        // pivot is at the bottom with the box 0.4 in front of it, so this sits it on the floor under the button
        private static readonly Vector3 CubeBodyOffset = new(0f, 0f, 0.4f);

        // its button is on the boxs back, turn it round so it faces the same way the shop button does
        private Quaternion CubeFacing => point.transform.rotation * Quaternion.Euler(0f, 180f, 0f);

        private Vector3 CubeSpot
        {
            get
            {
                Vector3 button = shopGrab.transform.position;
                var floor = new Vector3(button.x, station.transform.position.y, button.z);
                return floor - CubeFacing * Vector3.Scale(CubeBodyOffset, CubeScale);
            }
        }

        private static bool AnyoneWithoutTheMod()
        {
            foreach (Player player in PhotonNetwork.PlayerListOthers)
            {
                if (!Presence.HasMod(player))
                    return true;
            }
            return false;
        }

        // 0-1 for the bar, off the hosts settings
        private float PressProgress(bool canPress, bool sendEarly)
        {
            if (!canPress)
                return 0f;
            if (sendEarly)
                return 1f;
            return HaulProgress;
        }

        // empty till the hosts goal arrives, otherwise a 0 goal reads as full for a frame
        private float HaulProgress =>
            !point.haulGoalFetched ? 0f : point.haulGoal > 0 ? Mathf.Clamp01((float)point.haulCurrent / point.haulGoal) : 1f;

        // host side. no avatar counts as down too, so nothing waits on a host that isnt there
        private static bool HostIsDown()
        {
            if (!SemiFunc.IsMultiplayer())
                return false;
            PlayerAvatar host = PlayerAvatar.instance;
            return !host || host.isDisabled;
        }

        // one bar at a time, your own screen only. on the stand while the button is on (it hides with the stand,
        // so preview just shows the boxs own bar), at the back while its off
        private void ShowBars(float progress, bool onStand)
        {
            if (!barsTried)
            {
                barsTried = true;
                Transform pedestal = station.transform.Find("Extraction Point Side Button");
                standBar = ProgressBar.Build(pedestal, ProgressBar.StandCenter, Quaternion.identity);
                backBar = ProgressBar.Build(point.platform, ProgressBar.BackCenter, ProgressBar.BackFacing);
            }

            bool wanted = PressConfig.ProgressBar.Value;
            standBar?.SetVisible(wanted && onStand);
            backBar?.SetVisible(wanted && !onStand);
            ProgressBar? shown = onStand ? standBar : backBar;
            if (wanted)
                shown?.Show(progress);
        }

        // the boxs health bar is the same progress. the game sends any health change to everyone itself
        // (HealthUpdateRPC), so people without the mod see it fill. never 0, that sets it off
        private void ShowProgressOnCube(float progress)
        {
            if (cubeHealth == null)
                return;

            int health = Mathf.Max(1, Mathf.RoundToInt(progress * cubeHealth.healthMax));
            if (cubeHealth.healthCurrent != health)
                cubeHealth.healthCurrent = health;
        }

        // the common box, the games own spawn list has it
        internal static PrefabRef? SmallestCube()
        {
            foreach (ValuableDirector.CosmeticWorldObjectSetup setup in ValuableDirector.instance.cosmeticWorldObjectSetups)
            {
                if (setup.rarity == SemiFunc.Rarity.Common)
                    return setup.prefab;
            }
            return null;
        }

        private void TakeCube(CosmeticWorldObject cube)
        {
            this.cube = cube.physGrabObject;
            cubeHealth = cube.notValuableObject;
            cubeObject = cube.gameObject;

            cubeShown = null;
            ShowCube(!station.activeSelf);

            if (SemiFunc.IsMasterClientOrSingleplayer())
                cube.gameObject.AddComponent<CubePin>().Pin(this.cube, CubeSpot, CubeFacing);
        }

        // remember what we switched off so turning it back on doesnt light up bits the cube keeps off itself
        private void ShowCube(bool show)
        {
            if (cubeObject == null || cubeShown == show)
                return;
            cubeShown = show;

            if (show)
            {
                foreach (Renderer part in hiddenRenderers)
                    if (part) part.enabled = true;
                foreach (Collider part in hiddenColliders)
                    if (part) part.enabled = true;
                foreach (Light part in hiddenLights)
                    if (part) part.enabled = true;
                hiddenRenderers.Clear();
                hiddenColliders.Clear();
                hiddenLights.Clear();
                return;
            }

            foreach (Renderer part in cubeObject.GetComponentsInChildren<Renderer>(true))
            {
                if (!part.enabled) continue;
                part.enabled = false;
                hiddenRenderers.Add(part);
            }
            foreach (Collider part in cubeObject.GetComponentsInChildren<Collider>(true))
            {
                if (!part.enabled) continue;
                part.enabled = false;
                hiddenColliders.Add(part);
            }
            foreach (Light part in cubeObject.GetComponentsInChildren<Light>(true))
            {
                if (!part.enabled) continue;
                part.enabled = false;
                hiddenLights.Add(part);
            }
        }

        private void DropCube()
        {
            if (cubeObject && SemiFunc.IsMultiplayer())
                PhotonNetwork.Destroy(cubeObject);
            else if (cubeObject)
                Destroy(cubeObject);
            cubeObject = null;
            cube = null;
            cubeHealth = null;
            cubeShown = null;
            hiddenRenderers.Clear();
            hiddenColliders.Clear();
            hiddenLights.Clear();
        }

        private void TickAutoExtract(bool ready)
        {
            int after = PressConfig.AutoExtractAfter.Value;
            if (!ready || after <= 0)
            {
                goalMetFor = 0f;
                return;
            }

            goalMetFor += Time.deltaTime;
            if (goalMetFor >= after)
                Confirm();
        }

        private void Confirm()
        {
            goalMetFor = 0f;
            sentAt = Time.time;
            confirming = true;
            point.StateSet(ExtractionPoint.State.Success);
            confirming = false;
        }

        // drop the goal to whats on the pad so its a normal extraction, the rest gets owed once its sucking.
        // on the last point theres nowhere for it to go, so its just gone
        private void SendEarly()
        {
            fullGoal ??= point.haulGoal;
            SetGoal(point.haulCurrent);
            Confirm();
        }

        // canceled after sending early, give it the full goal back
        private void RestoreGoal()
        {
            if (fullGoal is not int goal)
                return;
            fullGoal = null;
            SetGoal(goal);
        }

        private void CommitShortfall()
        {
            if (fullGoal is not int goal)
                return;
            fullGoal = null;
            Shortfall.Add(PressRules.Owed(goal, point.haulGoal));
        }

        private void SetGoal(int goal)
        {
            settingGoal = true;
            point.HaulGoalSet(goal);
            settingGoal = false;
        }

        private void Cancel()
        {
            goalMetFor = 0f;
            point.StateSet(ExtractionPoint.State.Cancel);
        }

        // nope = the points own cancel (red X, cancel sound, back to filling a second later), same thing it does
        // when the haul drops mid countdown. only while filling, a cancel during the countdown would actualy cancel it
        private void Deny()
        {
            if (point.currentState == ExtractionPoint.State.Active)
                point.StateSet(ExtractionPoint.State.Cancel);
        }

        private void SetLit(bool on)
        {
            wantLit = on;
            // let the press squish finish, OnShopAnimationTick puts this back after
            if (!point.shopButtonAnimation)
                ApplyLit();
        }

        private void ApplyLit()
        {
            if (lit == wantLit)
                return;
            lit = wantLit;
            shopLight.enabled = wantLit;
            shopVisual.material.SetColor("_EmissionColor", wantLit ? ReadyColor : Color.black);
        }

        // READY on the extraction screen like an unopened point
        private void ShowReady(bool on)
        {
            // no early out when on, the game rewrites ACTIVE over it on a language change
            if (on)
            {
                screenReady = true;
                readyText ??= point.localizedExtractionReady ? point.localizedExtractionReady.GetLocalizedString() : "READY";
                TubeScreen.Show(point, readyText, ReadyColor);
                return;
            }

            if (!screenReady)
                return;
            screenReady = false;

            activeText ??= point.localizedExtractionActive ? point.localizedExtractionActive.GetLocalizedString() : "ACTIVE";
            TubeScreen.Show(point, activeText, Color.green, silent: true);
        }
    }
}
