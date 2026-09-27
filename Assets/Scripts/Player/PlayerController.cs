using UnityEngine;

namespace Eferin
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform cameraTransform;
        public float walkSpeed = 5.5f;
        public float sprintMultiplier = 1.7f;
        public float jumpHeight = 1.3f;
        public float gravity = -22f;
        public float turnSpeed = 12f;

        [Header("Выносливость")]
        public float staminaMax = 100f;
        public float sprintDrainPerSecond = 22f;
        public float staminaRegenPerSecond = 16f;

        public float Stamina { get; private set; }
        public bool IsSprinting { get; private set; }

        CharacterController controller;
        float verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            Stamina = staminaMax;
        }

        void Update()
        {
            Vector2 input = GameState.GameplayBlocked ? Vector2.zero : GameInput.Move();
            Vector3 move = Vector3.zero;

            if (input.sqrMagnitude > 0.01f && cameraTransform != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                move = (forward * input.y + right * input.x).normalized;

                Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            IsSprinting = move != Vector3.zero && GameInput.Held(GameKey.Sprint) && Stamina > 1f;
            Stamina = IsSprinting
                ? Mathf.Max(0f, Stamina - sprintDrainPerSecond * Time.deltaTime)
                : Mathf.Min(staminaMax, Stamina + staminaRegenPerSecond * Time.deltaTime);

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (!GameState.GameplayBlocked && GameInput.Pressed(GameKey.Jump))
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            float speed = walkSpeed * (IsSprinting ? sprintMultiplier : 1f);
            Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
