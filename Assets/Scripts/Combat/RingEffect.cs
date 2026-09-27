using UnityEngine;

namespace Eferin
{
    // Expanding, fading ground ring drawn with a LineRenderer — no prefab or texture needed.
    public class RingEffect : MonoBehaviour
    {
        const int Segments = 48;

        LineRenderer line;
        float radius;
        float life;
        float maxLife;
        Color color;

        public static void Spawn(Vector3 position, float radius, Color color, float duration)
        {
            var go = new GameObject("RingEffect");
            go.transform.position = position;
            var fx = go.AddComponent<RingEffect>();
            fx.Init(radius, color, duration);
        }

        void Init(float r, Color c, float duration)
        {
            radius = r;
            color = c;
            life = maxLife = duration;

            line = gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = MaterialFactory.Line();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Segments;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            Draw(0f);
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                Destroy(line.sharedMaterial);
                Destroy(gameObject);
                return;
            }
            Draw(1f - life / maxLife);
        }

        void Draw(float t)
        {
            float r = radius * Mathf.Lerp(0.4f, 1f, Mathf.Sqrt(t));
            for (int i = 0; i < Segments; i++)
            {
                float a = i / (float)Segments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            float width = Mathf.Lerp(0.35f, 0.05f, t);
            line.startWidth = line.endWidth = width;
            Color c = color;
            c.a = 1f - t;
            line.startColor = line.endColor = c;
        }
    }
}
