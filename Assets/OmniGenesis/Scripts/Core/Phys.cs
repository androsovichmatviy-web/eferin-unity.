using UnityEngine;

namespace OmniGenesis
{
    // Unity 6 hat Rigidbody.velocity/drag in linearVelocity/linearDamping umbenannt.
    public static class Phys
    {
        public static Vector3 GetVelocity(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(Rigidbody rb, Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        public static void SetDamping(Rigidbody rb, float linear, float angular)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = linear;
            rb.angularDamping = angular;
#else
            rb.drag = linear;
            rb.angularDrag = angular;
#endif
        }
    }

    public struct Strike
    {
        public float mass;      // kg
        public float accel;     // m/s²
        public float force;     // N
        public float damage;
        public float impulse;   // N·s
        public string formula;
    }

    // Der physikalische Kern des Kampfsystems: jeder Treffer wird über F = m · a berechnet.
    public static class PhysicsCombat
    {
        const float SwingArc = 2.4f; // überstrichener Winkel eines Schlags in rad

        // Nahkampf: Spitzengeschwindigkeit v = θ · L / t, Beschleunigung a = v / t.
        // Anlauf erhöht die Beschleunigung zusätzlich um v_anlauf / t. Dann F = m · a.
        public static Strike Melee(float weaponMass, float length, float swingTime, float approachSpeed, float multiplier)
        {
            float t = Mathf.Max(0.08f, swingTime);
            float tip = SwingArc * length / t;
            float a = tip / t + Mathf.Max(0f, approachSpeed) / t;
            return FromForce(weaponMass, a, multiplier, GameConstants.DamagePerNewton);
        }

        // Ätherzauber: eine Äthermasse wird durch die Intelligenz des Magiers beschleunigt.
        public static Strike Spell(float aetherMass, float accel, float multiplier)
        {
            return FromForce(aetherMass, accel, multiplier, GameConstants.DamagePerNewton);
        }

        // Körperangriff einer Kreatur: Körpermasse × Sprungbeschleunigung.
        public static Strike Body(float bodyMass, float accel, float multiplier)
        {
            return FromForce(bodyMass, accel, multiplier, GameConstants.DamagePerNewton);
        }

        static Strike FromForce(float m, float a, float multiplier, float perNewton)
        {
            var s = new Strike();
            s.mass = m;
            s.accel = a;
            s.force = m * a;
            s.damage = s.force * perNewton * multiplier;
            s.impulse = s.force * GameConstants.ImpactTime;
            s.formula = "F = " + m.ToString("0.00") + " kg · " + a.ToString("0") + " m/s² = " + s.force.ToString("0") + " N";
            return s;
        }

        // Bogen: Die Sehne drückt mit F auf den Pfeil, also a = F / m.
        // Über die Auszugslänge d gilt v² = 2 · a · d  =>  v = √(2 · F · d / m).
        public static float ArrowSpeed(float drawForce, float drawLength, float arrowMass)
        {
            return Mathf.Sqrt(2f * drawForce * drawLength / arrowMass);
        }

        // Pfeiltreffer: Schaden aus kinetischer Energie E = ½ · m · v², Impuls p = m · v.
        public static Strike ArrowImpact(float arrowMass, float speed, float multiplier)
        {
            var s = new Strike();
            float e = 0.5f * arrowMass * speed * speed;
            s.mass = arrowMass;
            s.accel = 0f;
            s.force = e;
            s.damage = e * GameConstants.DamagePerJoule * multiplier;
            s.impulse = arrowMass * speed;
            s.formula = "E = ½ · " + arrowMass.ToString("0.000") + " kg · (" + speed.ToString("0") + " m/s)² = " + e.ToString("0") + " J";
            return s;
        }
    }
}
