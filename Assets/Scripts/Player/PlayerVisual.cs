using UnityEngine;

namespace Eferin
{
    // Placeholder look for the active party member: a tinted capsule body plus a glowing Искра Отклика on the chest.
    public class PlayerVisual : MonoBehaviour
    {
        Material bodyMaterial;
        Material sparkMaterial;
        Transform spark;

        void Awake()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            bodyMaterial = MaterialFactory.Lit(Color.white, 0.3f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            head.transform.localScale = Vector3.one * 0.55f;
            head.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Lit(new Color(0.95f, 0.85f, 0.72f));

            var sparkGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sparkGo.name = "Spark";
            Destroy(sparkGo.GetComponent<Collider>());
            spark = sparkGo.transform;
            spark.SetParent(transform, false);
            spark.localPosition = new Vector3(0f, 1.3f, 0.42f);
            spark.localScale = Vector3.one * 0.2f;
            sparkMaterial = MaterialFactory.Glowing(Color.white);
            sparkGo.GetComponent<Renderer>().sharedMaterial = sparkMaterial;
        }

        void Start()
        {
            var party = GetComponent<PartyManager>();
            if (party == null) return;
            party.Switched += Apply;
            Apply(party.Active);
        }

        void Update()
        {
            spark.localRotation = Quaternion.Euler(45f, Time.time * 120f, 45f);
        }

        void Apply(CharacterData character)
        {
            Color c = Elements.ColorOf(character.element);
            MaterialFactory.SetColor(bodyMaterial, c);
            MaterialFactory.SetColor(sparkMaterial, c);
            MaterialFactory.SetEmission(sparkMaterial, c * 2.5f);
            RingEffect.Spawn(transform.position + Vector3.up * 0.1f, 1.2f, c, 0.35f);
        }
    }
}
