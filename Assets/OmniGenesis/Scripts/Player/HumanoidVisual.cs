using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OmniGenesis
{
    // Platzhalter-Figur aus Grundkörpern für Spieler und NPCs. Später durch Modelle aus Art/ ersetzbar.
    public class HumanoidVisual : MonoBehaviour
    {
        public float moveSpeed;
        public float swing = -1f;   // 0..1 während eines Schlags, sonst < 0

        Transform legL, legR, armL, armR, weaponPivot, root;
        readonly List<Renderer> bodyRenderers = new List<Renderer>();
        readonly List<GameObject> weaponParts = new List<GameObject>();
        float phase;
        bool firstPerson;
        Transform fpCamera;

        public static HumanoidVisual Build(Transform parent, Color cloth, Color accent, Color skin, float scale)
        {
            var go = new GameObject("Figur");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            var v = go.AddComponent<HumanoidVisual>();
            v.root = go.transform;
            v.Create(cloth, accent, skin);
            return v;
        }

        GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color, bool body = true)
        {
            var p = GameObject.CreatePrimitive(type);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            var r = p.GetComponent<Renderer>();
            r.sharedMaterial = MaterialFactory.Shared(color, 0.25f);
            if (body) bodyRenderers.Add(r);
            return p;
        }

        Transform Pivot(string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = pos;
            return t;
        }

        void Create(Color cloth, Color accent, Color skin)
        {
            legL = Pivot("BeinL", new Vector3(-0.13f, 0.9f, 0f));
            legR = Pivot("BeinR", new Vector3(0.13f, 0.9f, 0f));
            Part(PrimitiveType.Capsule, legL, new Vector3(0f, -0.45f, 0f), new Vector3(0.2f, 0.45f, 0.2f), accent * 0.8f);
            Part(PrimitiveType.Capsule, legR, new Vector3(0f, -0.45f, 0f), new Vector3(0.2f, 0.45f, 0.2f), accent * 0.8f);

            Part(PrimitiveType.Capsule, root, new Vector3(0f, 1.15f, 0f), new Vector3(0.5f, 0.42f, 0.32f), cloth);
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.93f, 0f), new Vector3(0.52f, 0.08f, 0.34f), accent);
            Part(PrimitiveType.Sphere, root, new Vector3(0f, 1.73f, 0f), Vector3.one * 0.32f, skin);

            armL = Pivot("ArmL", new Vector3(-0.33f, 1.48f, 0f));
            armR = Pivot("ArmR", new Vector3(0.33f, 1.48f, 0f));
            Part(PrimitiveType.Capsule, armL, new Vector3(0f, -0.3f, 0f), new Vector3(0.14f, 0.32f, 0.14f), cloth);
            Part(PrimitiveType.Capsule, armR, new Vector3(0f, -0.3f, 0f), new Vector3(0.14f, 0.32f, 0.14f), cloth);

            weaponPivot = new GameObject("Waffe").transform;
            weaponPivot.SetParent(armR, false);
            weaponPivot.localPosition = new Vector3(0f, -0.6f, 0.05f);
        }

        // Kopfbedeckung: Krone der Acht Sterne (Art. 20), Kapuze, Helm …
        public void AddCrown(Color gold)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                var spike = Part(PrimitiveType.Cube, root, new Vector3(Mathf.Cos(a) * 0.15f, 1.92f, Mathf.Sin(a) * 0.15f), new Vector3(0.05f, 0.12f, 0.05f), gold);
                spike.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Shared(gold, 0.7f, 1.2f);
            }
            Part(PrimitiveType.Cylinder, root, new Vector3(0f, 1.86f, 0f), new Vector3(0.34f, 0.03f, 0.34f), gold);
        }

        public void AddHelmet(Color c)
        {
            Part(PrimitiveType.Sphere, root, new Vector3(0f, 1.78f, 0f), new Vector3(0.36f, 0.26f, 0.36f), c);
        }

        public void AddRobe(Color c)
        {
            Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.32f, 0.4f), c);
        }

        public void SetWeapon(ItemDef w)
        {
            foreach (var p in weaponParts) if (p != null) Destroy(p);
            weaponParts.Clear();
            if (w == null || w.id == "faeuste") return;

            var steel = new Color(0.75f, 0.77f, 0.8f);
            var wood = new Color(0.45f, 0.3f, 0.18f);
            if (w.weaponKind == WeaponKind.Nahkampf)
            {
                float l = Mathf.Max(0.35f, w.length);
                bool heavy = w.mass > 2.5f;
                weaponParts.Add(Part(PrimitiveType.Cube, weaponPivot, new Vector3(0f, 0f, 0.08f), new Vector3(0.05f, 0.05f, 0.22f), wood, false));
                weaponParts.Add(Part(PrimitiveType.Cube, weaponPivot, new Vector3(0f, 0f, 0.2f + l * 0.5f), new Vector3(heavy ? 0.04f : 0.03f, heavy ? 0.2f : 0.08f, l), steel, false));
                if (w.id == "kriegsaxt")
                    weaponParts.Add(Part(PrimitiveType.Cube, weaponPivot, new Vector3(0f, 0.15f, 0.2f + l * 0.85f), new Vector3(0.04f, 0.3f, 0.22f), steel, false));
            }
            else if (w.weaponKind == WeaponKind.Stab)
            {
                weaponParts.Add(Part(PrimitiveType.Cylinder, weaponPivot, new Vector3(0f, 0f, 0.5f), new Vector3(0.05f, 0.8f, 0.05f), wood, false));
                weaponParts[0].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var crystal = Part(PrimitiveType.Sphere, weaponPivot, new Vector3(0f, 0f, 1.3f), Vector3.one * 0.14f, new Color(0.4f, 0.7f, 1f), false);
                crystal.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Shared(new Color(0.4f, 0.7f, 1f), 0.6f, 2.5f);
                weaponParts.Add(crystal);
            }
            else
            {
                for (int i = -2; i <= 2; i++)
                {
                    var seg = Part(PrimitiveType.Cube, weaponPivot, new Vector3(0f, i * 0.2f, 0.15f + Mathf.Abs(i) * -0.04f), new Vector3(0.04f, 0.22f, 0.04f), wood, false);
                    seg.transform.localRotation = Quaternion.Euler(i * 12f, 0f, 0f);
                    weaponParts.Add(seg);
                }
                weaponParts.Add(Part(PrimitiveType.Cube, weaponPivot, new Vector3(0f, 0f, 0.08f), new Vector3(0.01f, 0.95f, 0.01f), Color.white, false));
            }
            ApplyFirstPersonState();
        }

        public void SetFirstPerson(bool fp, Transform cam)
        {
            firstPerson = fp;
            fpCamera = cam;
            ApplyFirstPersonState();
        }

        void ApplyFirstPersonState()
        {
            foreach (var r in bodyRenderers)
                if (r != null) r.shadowCastingMode = firstPerson ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;

            if (firstPerson && fpCamera != null)
            {
                weaponPivot.SetParent(fpCamera, false);
                weaponPivot.localPosition = new Vector3(0.3f, -0.32f, 0.45f);
                weaponPivot.localRotation = Quaternion.identity;
                weaponPivot.localScale = Vector3.one * 0.8f;
            }
            else
            {
                weaponPivot.SetParent(armR, false);
                weaponPivot.localPosition = new Vector3(0f, -0.6f, 0.05f);
                weaponPivot.localRotation = Quaternion.identity;
                weaponPivot.localScale = Vector3.one;
            }
        }

        void Update()
        {
            float s = Mathf.Clamp01(moveSpeed / 5f);
            phase += Time.deltaTime * (4f + moveSpeed * 1.6f);
            float leg = Mathf.Sin(phase) * 35f * s;
            legL.localRotation = Quaternion.Euler(leg, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-leg, 0f, 0f);
            armL.localRotation = Quaternion.Euler(-leg * 0.6f, 0f, 0f);

            if (swing >= 0f)
            {
                float t = Mathf.Clamp01(swing);
                float e = t < 0.4f ? t / 0.4f : 1f;
                float back = t < 0.4f ? -Mathf.Lerp(0f, 130f, e) : Mathf.Lerp(-130f, 40f, (t - 0.4f) / 0.6f);
                armR.localRotation = Quaternion.Euler(back, 0f, 0f);
                if (firstPerson)
                    weaponPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-50f, 60f, t), Mathf.Lerp(25f, -35f, t), 0f);
            }
            else
            {
                armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.Euler(leg * 0.6f - 25f, 0f, 0f), 10f * Time.deltaTime);
                if (firstPerson)
                {
                    float bob = Mathf.Sin(phase * 0.5f) * 0.015f * s;
                    weaponPivot.localPosition = new Vector3(0.3f, -0.32f + bob, 0.45f);
                    weaponPivot.localRotation = Quaternion.Slerp(weaponPivot.localRotation, Quaternion.Euler(-10f, 0f, 0f), 10f * Time.deltaTime);
                }
            }
        }

        void OnDestroy()
        {
            if (weaponPivot != null && weaponPivot.parent != armR) Destroy(weaponPivot.gameObject);
        }
    }
}
