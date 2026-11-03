using UnityEngine;

namespace OmniGenesis
{
    // Einfache Effekte ohne Prefabs: Ringe, Scheiben (Vorwarnung), Splitter.
    public class Fx : MonoBehaviour
    {
        enum Mode { Ring, Disc, Shard }

        Mode mode;
        float life, maxLife, radius;
        Color color;
        Vector3 velocity;
        LineRenderer line;
        Renderer rend;
        Material ownMaterial;

        const int Segments = 40;

        public static void Ring(Vector3 pos, float radius, Color color, float duration)
        {
            var go = new GameObject("Fx-Ring");
            go.transform.position = pos;
            var fx = go.AddComponent<Fx>();
            fx.mode = Mode.Ring;
            fx.radius = radius;
            fx.color = color;
            fx.life = fx.maxLife = duration;
            fx.line = go.AddComponent<LineRenderer>();
            fx.ownMaterial = MaterialFactory.Effect();
            fx.line.sharedMaterial = fx.ownMaterial;
            fx.line.useWorldSpace = false;
            fx.line.loop = true;
            fx.line.positionCount = Segments;
            fx.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx.DrawRing(0f);
        }

        // Vorwarnung eines Angriffs: wachsende Scheibe am Boden.
        public static void Disc(Vector3 pos, float radius, Color color, float duration)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = "Fx-Warnung";
            go.transform.position = pos + Vector3.up * 0.08f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var fx = go.AddComponent<Fx>();
            fx.mode = Mode.Disc;
            fx.radius = radius;
            fx.color = color;
            fx.life = fx.maxLife = duration;
            fx.rend = go.GetComponent<Renderer>();
            fx.ownMaterial = MaterialFactory.Effect(ProcTex.Circle);
            fx.rend.sharedMaterial = fx.ownMaterial;
            fx.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx.UpdateDisc(0f);
        }

        public static void Burst(Vector3 pos, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.name = "Fx-Splitter";
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
                go.transform.rotation = Random.rotation;
                go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Shared(color, 0.3f, 1f);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var fx = go.AddComponent<Fx>();
                fx.mode = Mode.Shard;
                fx.life = fx.maxLife = Random.Range(0.35f, 0.7f);
                fx.velocity = Random.insideUnitSphere * 4f + Vector3.up * 3f;
            }
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                if (ownMaterial != null) Destroy(ownMaterial);
                Destroy(gameObject);
                return;
            }
            float t = 1f - life / maxLife;
            switch (mode)
            {
                case Mode.Ring: DrawRing(t); break;
                case Mode.Disc: UpdateDisc(t); break;
                case Mode.Shard:
                    velocity += Physics.gravity * Time.deltaTime;
                    transform.position += velocity * Time.deltaTime;
                    transform.localScale *= 1f - Time.deltaTime * 1.5f;
                    break;
            }
        }

        void DrawRing(float t)
        {
            float r = radius * Mathf.Lerp(0.3f, 1f, Mathf.Sqrt(t));
            for (int i = 0; i < Segments; i++)
            {
                float a = i / (float)Segments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            line.startWidth = line.endWidth = Mathf.Lerp(0.3f, 0.04f, t);
            Color c = color;
            c.a = 1f - t;
            line.startColor = line.endColor = c;
        }

        void UpdateDisc(float t)
        {
            float s = radius * 2f * Mathf.Lerp(0.2f, 1f, t);
            transform.localScale = new Vector3(s, s, 1f);
            Color c = color;
            c.a = Mathf.Lerp(0.15f, 0.55f, t);
            ownMaterial.color = c;
        }
    }
}
