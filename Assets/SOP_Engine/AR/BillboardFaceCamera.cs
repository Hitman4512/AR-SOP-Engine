using UnityEngine;

namespace SOP_Engine.AR
{
    /// <summary>
    /// Keeps the object facing the camera (useful for labels).
    /// </summary>
    public class BillboardFaceCamera : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool lockPitch = true;

        private void LateUpdate()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            var dir = transform.position - cam.transform.position;
            if (lockPitch) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
