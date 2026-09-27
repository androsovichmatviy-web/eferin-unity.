using UnityEngine;

namespace Eferin
{
    // Genshin-style orbit camera: the mouse always steers the view, hold Left Alt to free the cursor.
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
        public float distance = 6f;
        public float minDistance = 2.5f;
        public float maxDistance = 12f;
        public float sensitivity = 3f;
        public float zoomStep = 1f;
        public float minPitch = -20f;
        public float maxPitch = 70f;
        public float collisionRadius = 0.25f;

        float yaw;
        float pitch = 15f;
        float currentDistance;

        void Start()
        {
            if (target != null) yaw = target.eulerAngles.y;
            currentDistance = distance;
        }

        void LateUpdate()
        {
            if (target == null) return;

            bool cursorFree = GameState.GameplayBlocked || GameInput.Held(GameKey.ShowCursor);
            Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = cursorFree;

            if (!cursorFree)
            {
                Vector2 look = GameInput.LookDelta();
                yaw += look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
                distance = Mathf.Clamp(distance - GameInput.Scroll() * zoomStep, minDistance, maxDistance);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + targetOffset;
            Vector3 dir = rotation * Vector3.back;

            float wanted = distance;
            if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore)
                && hit.transform != target)
            {
                wanted = Mathf.Max(0.5f, hit.distance);
            }
            currentDistance = Mathf.Lerp(currentDistance, wanted, 12f * Time.deltaTime);

            transform.position = pivot + dir * currentDistance;
            transform.rotation = rotation;
        }
    }
}
