using UnityEngine;

namespace Eferin
{
    // Drop this on an empty GameObject in a Basic (URP) scene and press Play — it builds the whole prototype.
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Vector3 spawn = WorldBuilder.Build();

            var player = new GameObject("Player");
            player.transform.SetPositionAndRotation(spawn, Quaternion.identity);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.4f;
            controller.slopeLimit = 50f;

            player.AddComponent<PartyManager>();
            player.AddComponent<PlayerVisual>();
            var movement = player.AddComponent<PlayerController>();
            player.AddComponent<CombatController>();

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.farClipPlane = 400f;
            cam.fieldOfView = 55f;

            if (!cam.TryGetComponent(out ThirdPersonCamera orbit))
                orbit = cam.gameObject.AddComponent<ThirdPersonCamera>();
            orbit.target = player.transform;
            movement.cameraTransform = cam.transform;

            EnemySpawner.SpawnAll(player.transform);

            gameObject.AddComponent<GameHud>();
            gameObject.AddComponent<DialogueSystem>();
            gameObject.AddComponent<Codex>();
        }
    }
}
