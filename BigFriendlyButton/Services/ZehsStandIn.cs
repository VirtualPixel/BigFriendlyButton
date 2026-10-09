using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace BigFriendlyButton.Services
{
    // hosting with Zehs ExtractionPointConfirmButton spawns his button for everyone through REPOLib. without his
    // mod that spawn just fails, so we build a look alike: same two photon views in the same order (root + button)
    // and the same rpc names. our presses reach his host as normal grabs on his button and his code takes it from there
    internal sealed class ZehsStandIn : MonoBehaviour
    {
        private const string PrefabId = "ConfirmButton";

        private static readonly List<ZehsStandIn> Spawned = new();

        private ExtractionPoint? point;
        private GameObject body = null!;
        private Transform visual = null!;
        private MeshRenderer visualRenderer = null!;
        private float pressEval = -1f;

        // on whatever Enabled says, its his button on his host
        public static bool IsZehsButton(string prefabId) =>
            prefabId == PrefabId && !Gate.ZehsLoaded;

        // the pool wants it back inactive, PUN numbers the views and turns it on
        public static GameObject? Build(Vector3 position, Quaternion rotation)
        {
            Transform? template = ExtractionButton.AnyShopStation();
            if (template == null)
                return null;

            var root = new GameObject(PrefabId);
            root.SetActive(false);
            root.transform.SetPositionAndRotation(position, rotation);
            root.AddComponent<PhotonView>();
            ZehsStandIn standIn = root.AddComponent<ZehsStandIn>();

            Transform stand = Instantiate(template, root.transform, false);
            stand.gameObject.SetActive(true);
            stand.localPosition = ExtractionButton.StandOffset;
            stand.localRotation = Quaternion.identity;
            stand.localScale = ExtractionButton.StandScale;

            // copied views start at id 0 (unity doesnt copy it) and PUN numbers them root first, same as his prefab
            StaticGrabObject grab = stand.GetComponentInChildren<StaticGrabObject>(true);

            Transform visual = grab.transform.Find("Shop Button Visual");
            if (!visual)
                visual = grab.transform;
            standIn.body = stand.gameObject;
            standIn.visual = visual;
            standIn.visualRenderer = visual.GetComponent<MeshRenderer>();

            // zehs button is always lit
            foreach (Light light in grab.GetComponentsInChildren<Light>(true))
                light.enabled = true;
            standIn.visualRenderer.material.SetColor("_EmissionColor", ExtractionButton.ReadyColor);
            return root;
        }

        public static void OnStateSet(ExtractionPoint point, ExtractionPoint.State state)
        {
            foreach (ZehsStandIn standIn in Spawned)
            {
                if (standIn.point == point)
                    standIn.body.SetActive(ShowsIn(state));
            }
        }

        // same states his button hides in
        private static bool ShowsIn(ExtractionPoint.State state) =>
            state is not (ExtractionPoint.State.Idle or ExtractionPoint.State.Extracting
                or ExtractionPoint.State.Complete or ExtractionPoint.State.TaxReturn);

        private void OnEnable() => Spawned.Add(this);

        private void OnDisable() => Spawned.Remove(this);

        [PunRPC]
        private void SyncExtractionPointRPC(int extractionPointViewID, PhotonMessageInfo _info = default)
        {
            if (!SemiFunc.MasterOnlyRPC(_info))
                return;
            PhotonView view = PhotonView.Find(extractionPointViewID);
            if (!view || !view.TryGetComponent(out ExtractionPoint found))
                return;

            point = found;
            ExtractionPoint.State state = found.stateSetTo != ExtractionPoint.State.None ? found.stateSetTo : found.currentState;
            body.SetActive(ShowsIn(state));
        }

        [PunRPC]
        private void OnClickRPC(PhotonMessageInfo _info = default)
        {
            if (!SemiFunc.MasterOnlyRPC(_info) || point == null || pressEval >= 0f)
                return;
            visual.localScale = new Vector3(1f, 0.1f, 1f);
            point.soundButton.Play(visual.position);
            pressEval = 0f;
        }

        // his press squish, which is the shops
        private void Update()
        {
            if (pressEval < 0f || point == null)
                return;

            pressEval = Mathf.Clamp01(pressEval + Time.deltaTime * 2f);
            float curve = point.buttonPressAnimationCurve.Evaluate(pressEval);
            visualRenderer.material.SetColor("_EmissionColor", Color.Lerp(ExtractionButton.ReadyColor, Color.white, curve));
            visual.localScale = new Vector3(1f, Mathf.Clamp(curve, 0.5f, 1f), 1f);
            if (pressEval >= 1f)
                pressEval = -1f;
        }
    }
}
