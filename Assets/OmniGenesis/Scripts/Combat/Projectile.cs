using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Geschoss mit eigener Bahnberechnung (Pfeile mit Schwerkraft, Ätherbolzen geradlinig oder zielsuchend).
    public class Projectile : MonoBehaviour
    {
        public Health owner;
        public Func<Health, bool> canHit;
        public Action<Health, Projectile> onHit;
        public Action<Vector3> onExplode;
        public float explodeRadius;
        public int pierce;
        public bool gravity;
        public Health homingTarget;
        public float life = 4f;
        public float radius = 0.15f;

        public Vector3 Velocity { get { return velocity; } }

        Vector3 velocity;
        readonly List<Health> alreadyHit = new List<Health>();
        TrailRenderer trail;

        public static Projectile Spawn(Health owner, Vector3 pos, Vector3 velocity, Color color, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Geschoss";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size, size, size * (velocity.magnitude > 40f ? 4f : 1.5f));
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Shared(color, 0.5f, 2f);
            var p = go.AddComponent<Projectile>();
            p.owner = owner;
            p.velocity = velocity;
            p.radius = Mathf.Max(0.1f, size * 0.6f);

            p.trail = go.AddComponent<TrailRenderer>();
            p.trail.sharedMaterial = MaterialFactory.Effect();
            p.trail.time = 0.15f;
            p.trail.startWidth = size * 0.8f;
            p.trail.endWidth = 0f;
            p.trail.startColor = new Color(color.r, color.g, color.b, 0.8f);
            p.trail.endColor = new Color(color.r, color.g, color.b, 0f);
            p.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Finish(transform.position, false); return; }

            if (homingTarget != null && !homingTarget.IsDead)
            {
                Vector3 want = (homingTarget.Center - transform.position).normalized * velocity.magnitude;
                velocity = Vector3.RotateTowards(velocity, want, 4f * dt, 0f);
            }
            if (gravity) velocity += Physics.gravity * dt;

            Vector3 from = transform.position;
            Vector3 step = velocity * dt;
            float dist = step.magnitude;
            if (dist > 0.0001f)
            {
                var hits = Physics.SphereCastAll(from, radius, step / dist, dist, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    var hp = h.collider.GetComponentInParent<Health>();
                    if (hp != null && (hp == owner || alreadyHit.Contains(hp))) continue;
                    if (hp != null && hp.IsDead) continue;
                    if (hp != null && canHit != null && !canHit(hp)) continue;

                    Vector3 point = h.distance > 0f ? from + step.normalized * h.distance : from;
                    if (hp != null)
                    {
                        alreadyHit.Add(hp);
                        if (onHit != null) onHit(hp, this);
                        if (pierce > 0) { pierce--; continue; }
                    }
                    Finish(point, true);
                    return;
                }
            }

            transform.position = from + step;
            if (velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
        }

        void Finish(Vector3 point, bool impact)
        {
            if (explodeRadius > 0f && onExplode != null) onExplode(point);
            else if (impact) Fx.Burst(point, new Color(0.9f, 0.9f, 0.9f), 4);
            if (trail != null) trail.transform.SetParent(null);
            Destroy(gameObject);
            if (trail != null) Destroy(trail.gameObject, 0.3f);
        }
    }
}
