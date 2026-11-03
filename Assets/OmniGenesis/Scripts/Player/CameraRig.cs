using UnityEngine;

namespace OmniGenesis
{
    // Kamera aus der Ich-Perspektive und aus der dritten Person. Umschalten mit V.
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public float sensitivity = 3f;
        public float distance = 5.5f;
        public float minDistance = 2f;
        public float maxDistance = 12f;

        public bool FirstPerson { get; private set; }
        public float Yaw { get { return yaw; } }
        public Camera Cam { get; private set; }

        float yaw;
        float pitch = 12f;
        float currentDistance;
        float shake;
        float eyeHeight = 1.65f;

        void Awake()
        {
            Cam = GetComponent<Camera>();
            currentDistance = distance;
        }

        public void Init(Transform player, bool firstPerson, float eye)
        {
            target = player;
            eyeHeight = eye;
            yaw = player.eulerAngles.y;
            SetFirstPerson(firstPerson);
        }

        public void SetYaw(float y) { yaw = y; }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        public void SetFirstPerson(bool fp)
        {
            FirstPerson = fp;
            var pc = World.Player;
            if (pc != null && pc.visual != null) pc.visual.SetFirstPerson(fp, transform);
            if (Cam != null) Cam.nearClipPlane = fp ? 0.05f : 0.2f;
        }

        void LateUpdate()
        {
            if (target == null) return;

            bool free = World.GameplayBlocked || GameInput.Held(Act.ShowCursor);
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;

            if (!free)
            {
                Vector2 look = GameInput.LookDelta();
                yaw += look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, FirstPerson ? -80f : -25f, FirstPerson ? 80f : 70f);
                if (!FirstPerson) distance = Mathf.Clamp(distance - GameInput.Scroll(), minDistance, maxDistance);
                if (GameInput.Pressed(Act.CameraToggle))
                {
                    SetFirstPerson(!FirstPerson);
                    Notify.System(FirstPerson ? "Ansicht: Ich-Perspektive" : "Ansicht: Dritte Person");
                }
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = shake > 0f ? Random.insideUnitSphere * shake * 0.15f : Vector3.zero;
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 3f);

            if (FirstPerson)
            {
                transform.position = target.position + Vector3.up * eyeHeight + offset;
                transform.rotation = rot;
                return;
            }

            Vector3 pivot = target.position + Vector3.up * (eyeHeight - 0.05f);
            Vector3 dir = rot * Vector3.back;
            float wanted = distance;
            var hits = Physics.SphereCastAll(pivot, 0.25f, dir, distance, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.transform.root == target.root) continue;
                if (h.distance < wanted) wanted = Mathf.Max(0.6f, h.distance);
            }
            currentDistance = Mathf.Lerp(currentDistance, wanted, 12f * Time.deltaTime);
            transform.position = pivot + dir * currentDistance + offset;
            transform.rotation = rot;
        }

        // Zielpunkt in der Bildschirmmitte (Fadenkreuz), ohne den Spieler selbst zu treffen.
        public Vector3 AimPoint(float maxDistance)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            float best = maxDistance;
            var hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (target != null && h.transform.root == target.root) continue;
                if (h.distance < best) best = h.distance;
            }
            return ray.origin + ray.direction * best;
        }

        public Health AimTarget(float maxDistance)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            Health best = null;
            float bestD = maxDistance;
            var hits = Physics.SphereCastAll(ray, 0.6f, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (target != null && h.transform.root == target.root) continue;
                var hp = h.collider.GetComponentInParent<Health>();
                if (hp == null || hp.IsDead) continue;
                if (h.distance < bestD)
                {
                    bestD = h.distance;
                    best = hp;
                }
            }
            return best;
        }
    }
}
