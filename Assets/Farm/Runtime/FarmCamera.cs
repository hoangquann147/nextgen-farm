using UnityEngine;

namespace NextGenFarm
{
    public sealed class FarmCamera : MonoBehaviour
    {
        public Transform target;
        public bool FreezeFollow { get; set; }
        public bool MenuOrbit { get; set; }
        private float zoom = 0.08f;
        private float yaw, pitchOffset;
        public void AdjustZoom(float amount) { zoom = Mathf.Clamp01(zoom + amount); }
        private void LateUpdate()
        {
            if (target == null || FreezeFollow) return;
            if (MenuOrbit) yaw = Mathf.Repeat(yaw + Time.unscaledDeltaTime * 2.5f, 360);
            else
            {
                zoom = Mathf.Clamp01(zoom - FarmInput.Scroll * 0.08f);
                Vector2 look=FarmInput.Look; yaw=Mathf.Repeat(yaw+look.x*.12f,360); pitchOffset=Mathf.Clamp(pitchOffset-look.y*.08f,-8,45);
            }
            float pitch = MenuOrbit ? 28 : Mathf.Clamp(Mathf.Lerp(11, 66, zoom)+pitchOffset, 5, 82);
            float distance = MenuOrbit ? 30 : Mathf.Lerp(5.5f, 32, zoom);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 destination = target.position + Vector3.up * 1.3f - rotation * Vector3.forward * distance;
            float blend = 1 - Mathf.Exp(-7 * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, destination, blend);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, blend);
        }
    }
}
