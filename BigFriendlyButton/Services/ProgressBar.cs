using UnityEngine;

namespace BigFriendlyButton.Services
{
    // just the health bar off the cosmetic box (black backing + the fill). one goes on the front of the shop stand
    // while the button is on, one at the back by the extraction screen (where ZeroGravityExtraction puts its
    // switch) for when it isnt. both sit on things the game only shows while the point is open
    internal sealed class ProgressBar
    {
        // the box draws it at 1.65, its pivot is the left end
        private const float BarScale = 1.65f;
        private const float MinFill = 0.05f;

        // on the stands front panel under the button. its flat at z -0.474 from about 0.5 to 0.9 up (raycast the mesh),
        // this puts the bar a few mm proud of it. the bar mesh sits 0.0165 in front of its own pivot
        public static readonly Vector3 StandCenter = new(0f, 0.78f, -0.4635f);

        // back of the pad, turned round to face it
        public static readonly Vector3 BackCenter = new(0f, 1.55f, -1.5f);
        public static readonly Quaternion BackFacing = Quaternion.Euler(0f, 180f, 0f);

        private readonly GameObject root;
        private readonly Transform fill;
        private readonly Material fillMaterial;
        private readonly Gradient gradient;
        private float shown = -1f;

        private ProgressBar(GameObject root, Transform fill, Material fillMaterial, Gradient gradient)
        {
            this.root = root;
            this.fill = fill;
            this.fillMaterial = fillMaterial;
            this.gradient = gradient;
        }

        public static ProgressBar? Build(Transform parent, Vector3 center, Quaternion facing)
        {
            PrefabRef? prefab = ExtractionButton.SmallestCube();
            if (!parent || prefab == null || !prefab.Prefab || !prefab.Prefab.TryGetComponent(out CosmeticWorldObject box))
                return null;

            Transform source = box.health.healthTransform;
            Transform bar = Object.Instantiate(source.parent, parent, false);
            bar.localRotation = facing;
            bar.localScale = Vector3.one * BarScale;
            bar.localPosition = center - facing * new Vector3(0.09f * BarScale, 0f, 0f);

            Transform fill = bar.Find(source.name);
            MeshRenderer renderer = fill.GetComponentInChildren<MeshRenderer>();
            return new ProgressBar(bar.gameObject, fill, renderer.material, box.healthGradient);
        }

        public void SetVisible(bool visible)
        {
            if (root.activeSelf != visible)
                root.SetActive(visible);
        }

        // color off the boxs own gradient
        public void Show(float progress)
        {
            shown = shown < 0f ? progress : Mathf.Lerp(shown, progress, Time.deltaTime * 8f);
            // a red stub even when empty, a bare black strip just looks like part of the stand
            fill.localScale = new Vector3(Mathf.Max(MinFill, shown), fill.localScale.y, fill.localScale.z);
            Color color = gradient.Evaluate(shown);
            fillMaterial.SetColor("_Color", color);
            fillMaterial.SetColor("_EmissionColor", color);
        }
    }
}
