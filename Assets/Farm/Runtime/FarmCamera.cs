using UnityEngine;

namespace NextGenFarm
{
    public sealed class FarmCamera : MonoBehaviour
    {
        public Transform target;
        public bool FreezeFollow { get; set; }
        private float zoom = 0.08f;
        private float yaw, pitchOffset;
        public void AdjustZoom(float amount) { zoom = Mathf.Clamp01(zoom + amount); }
        private void LateUpdate()
        {
            if (target == null || FreezeFollow) return;
            zoom = Mathf.Clamp01(zoom - FarmInput.Scroll * 0.08f);
            Vector2 look=FarmInput.Look; yaw+=look.x*.12f; pitchOffset=Mathf.Clamp(pitchOffset-look.y*.08f,-8,12);
            float pitch = Mathf.Lerp(11, 66, zoom)+pitchOffset;
            float distance = Mathf.Lerp(5.5f, 32, zoom);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 destination = target.position + Vector3.up * 1.3f - rotation * Vector3.forward * distance;
            float blend = 1 - Mathf.Exp(-7 * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, destination, blend);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, blend);
        }
    }
}
