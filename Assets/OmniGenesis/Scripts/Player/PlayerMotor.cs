using UnityEngine;

namespace OmniGenesis
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        public float walkSpeed = 4.6f;
        public float sprintMultiplier = 1.6f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;
        public float turnSpeed = 12f;

        public Vector3 Velocity { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool Grounded { get { return controller != null && controller.isGrounded; } }

        CharacterController controller;
        PlayerCharacter pc;
        Vector3 external;
        float verticalVelocity;
        float faceAimUntil;
        float speedMult = 1f;
        float speedMultUntil;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            pc = GetComponent<PlayerCharacter>();
        }

        public void FaceAim(float seconds)
        {
            faceAimUntil = Time.time + seconds;
            if (World.Rig != null) transform.rotation = Quaternion.Euler(0f, World.Rig.Yaw, 0f);
        }

        public void Slow(float mult, float seconds)
        {
            speedMult = mult;
            speedMultUntil = Time.time + seconds;
        }

        // Rückstoß als Geschwindigkeitsänderung Δv = J / m.
        public void AddImpulse(Vector3 impulse)
        {
            if (pc != null && pc.HasBuffFlag(b => b.knockbackImmune)) return;
            float m = pc != null ? pc.health.mass : 80f;
            external += impulse / m;
        }

        public void Dash(Vector3 velocity)
        {
            external += velocity;
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            controller.enabled = false;
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            controller.enabled = true;
            verticalVelocity = 0f;
            external = Vector3.zero;
            if (World.Rig != null) World.Rig.SetYaw(yaw);
        }

        void Update()
        {
            if (!World.Ready || pc == null || pc.health.IsDead)
            {
                Velocity = Vector3.zero;
                return;
            }

            bool blocked = World.GameplayBlocked;
            Vector2 input = blocked ? Vector2.zero : GameInput.Move();
            float yaw = World.Rig != null ? World.Rig.Yaw : transform.eulerAngles.y;
            Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 move = Vector3.zero;
            if (input.sqrMagnitude > 0.01f) move = (yawRot * new Vector3(input.x, 0f, input.y)).normalized;

            bool firstPerson = World.Rig != null && World.Rig.FirstPerson;
            if (firstPerson || Time.time < faceAimUntil)
                transform.rotation = Quaternion.Slerp(transform.rotation, yawRot, 20f * Time.deltaTime);
            else if (move != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);

            IsSprinting = move != Vector3.zero && !blocked && GameInput.Held(Act.Sprint) && pc.stamina > 1f;
            if (IsSprinting) pc.stamina = Mathf.Max(0f, pc.stamina - 20f * Time.deltaTime);

            if (Time.time > speedMultUntil) speedMult = 1f;
            float speed = walkSpeed * (1f + pc.Agi * 0.004f) * pc.EncumbranceFactor * speedMult * (IsSprinting ? sprintMultiplier : 1f);

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (!blocked && GameInput.Pressed(Act.Jump)) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 horizontal = move * speed + external;
            controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
            Velocity = horizontal;
            external = Vector3.Lerp(external, Vector3.zero, 6f * Time.deltaTime);

            if (pc.visual != null) pc.visual.moveSpeed = new Vector2(horizontal.x, horizontal.z).magnitude;

            if (transform.position.y < -60f) Teleport(WorldBootstrap.SafeSpawn(), 0f);
        }

        // Schiebt Körper mit F = m · a weg, wenn der Spieler hineinläuft.
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.rigidbody;
            if (rb == null || rb.isKinematic || hit.moveDirection.y < -0.3f) return;
            Vector3 dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            rb.AddForce(dir * pc.health.mass * 6f, ForceMode.Force);
        }
    }
}
