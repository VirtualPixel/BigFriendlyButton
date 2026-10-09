using UnityEngine;

namespace BigFriendlyButton.Services
{
    // host side, keeps the cube bolted to the button spot and unbreakable. physics is the hosts so everyone sees it stay put
    internal sealed class CubePin : MonoBehaviour
    {
        private PhysGrabObject grab = null!;
        private Vector3 position;
        private Quaternion rotation;

        public void Pin(PhysGrabObject cube, Vector3 at, Quaternion facing)
        {
            grab = cube;
            position = at;
            rotation = facing;
            // hurt colliders destroy things even when theyre indestructible, this is the one flag they check
            cube.impactDetector.destroyDisable = true;
        }

        private void FixedUpdate()
        {
            grab.OverrideKinematic(0.2f);
            grab.OverrideIndestructible(0.2f);
            if (transform.position == position && transform.rotation == rotation)
                return;
            grab.rb.position = position;
            grab.rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
