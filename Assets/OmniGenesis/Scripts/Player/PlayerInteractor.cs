using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Alles, womit man mit [F] interagieren kann: NPCs, Ressourcen, Tore, Betten, Truhen …
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public float radius = 3f;
        public Vector3 offset = Vector3.up;

        public abstract string Prompt { get; }
        public virtual bool CanInteract { get { return true; } }
        public abstract void Interact(PlayerCharacter player);

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public Vector3 Point { get { return transform.position + offset; } }
    }

    public class PlayerInteractor : MonoBehaviour
    {
        public Interactable Current { get; private set; }
        PlayerCharacter pc;

        void Awake() { pc = GetComponent<PlayerCharacter>(); }

        void Update()
        {
            Current = null;
            if (!World.Ready || World.GameplayBlocked || pc.health.IsDead) return;

            Vector3 eye = transform.position + Vector3.up * 1.2f;
            Vector3 look = World.Rig != null ? World.Rig.transform.forward : transform.forward;
            look.y = 0f;
            float best = float.MaxValue;
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                var it = Interactable.All[i];
                if (it == null || !it.isActiveAndEnabled || !it.CanInteract) continue;
                Vector3 d = it.Point - eye;
                float dist = d.magnitude;
                if (dist > it.radius) continue;
                d.y = 0f;
                float facing = d.sqrMagnitude > 0.01f ? Vector3.Dot(look.normalized, d.normalized) : 1f;
                if (facing < -0.2f && dist > 1.2f) continue;
                float score = dist - facing;
                if (score < best)
                {
                    best = score;
                    Current = it;
                }
            }

            if (Current != null && GameInput.Pressed(Act.Interact) && Current.CanInteract) Current.Interact(pc);
        }
    }
}
