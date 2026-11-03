using UnityEngine;

namespace OmniGenesis
{
    public enum WeatherKind { Klar, Bewoelkt, Regen, Gewitter, Nebel }

    // Wetter als Markow-Kette. Es wirkt auf Licht, Sicht, Wachstum, Bögen und Handel.
    public class Weather : MonoBehaviour
    {
        public WeatherKind Current { get; private set; }
        public float LightMult { get; private set; }

        float nextChange;
        float fogDensity = 0.003f;
        float flash;
        float nextThunder;
        ParticleSystem rain;
        Material rainMaterial;

        public float BowMult { get { return Current == WeatherKind.Regen || Current == WeatherKind.Gewitter ? 0.9f : 1f; } }
        public float AggroMult { get { return Current == WeatherKind.Nebel ? 0.6f : 1f; } }
        public float ProductionMult { get { return Current == WeatherKind.Gewitter ? 0.3f : 1f; } }
        public float GrowthMult { get { return Current == WeatherKind.Regen || Current == WeatherKind.Gewitter ? 2f : 1f; } }
        public bool IsStorm { get { return Current == WeatherKind.Gewitter; } }
        public float Flash { get { return flash; } }

        public static string Name(WeatherKind k)
        {
            switch (k)
            {
                case WeatherKind.Klar: return "Klar";
                case WeatherKind.Bewoelkt: return "Bewölkt";
                case WeatherKind.Regen: return "Regen";
                case WeatherKind.Gewitter: return "Gewitter";
                default: return "Nebel";
            }
        }

        public void Init(int saved, WorldClock clock)
        {
            Current = (WeatherKind)Mathf.Clamp(saved, 0, 4);
            LightMult = TargetLight();
            nextChange = clock.minutes + Random.Range(180f, 360f);
            clock.HourPassed += OnHour;
            CreateRain();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        void CreateRain()
        {
            var go = new GameObject("Regen");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            rain = go.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 24f;
            main.startSize = 0.04f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.7f, 0.8f, 1f, 0.45f);
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(45f, 45f, 1f);
            var emission = rain.emission;
            emission.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 6f;
            rainMaterial = MaterialFactory.Effect();
            r.sharedMaterial = rainMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rain.Play();
        }

        void OnHour(int day, int hour)
        {
            if (World.Clock.minutes < nextChange) return;
            nextChange = World.Clock.minutes + Random.Range(180f, 360f);
            var next = Pick(Current);
            if (next == WeatherKind.Nebel && (hour > 10 && hour < 19)) next = WeatherKind.Bewoelkt;
            if (next == Current) return;
            Current = next;
            Notify.Post("[Wetter] " + Describe(next), MessageKind.System);
        }

        static string Describe(WeatherKind k)
        {
            switch (k)
            {
                case WeatherKind.Klar: return "Der Himmel klart auf.";
                case WeatherKind.Bewoelkt: return "Wolken ziehen auf.";
                case WeatherKind.Regen: return "Es regnet. Kräuter und Bäume wachsen doppelt so schnell, Bogensehnen werden nass (−10 %).";
                case WeatherKind.Gewitter: return "Gewitter! Karawanen stocken (Handel −70 %), Tiere suchen Schutz.";
                default: return "Nebel zieht auf. Tiere bemerken dich später (−40 % Wahrnehmung).";
            }
        }

        static WeatherKind Pick(WeatherKind from)
        {
            float r = Random.value;
            switch (from)
            {
                case WeatherKind.Klar:
                    return r < 0.45f ? WeatherKind.Klar : r < 0.8f ? WeatherKind.Bewoelkt : WeatherKind.Nebel;
                case WeatherKind.Bewoelkt:
                    return r < 0.3f ? WeatherKind.Klar : r < 0.55f ? WeatherKind.Bewoelkt : r < 0.85f ? WeatherKind.Regen : r < 0.95f ? WeatherKind.Gewitter : WeatherKind.Nebel;
                case WeatherKind.Regen:
                    return r < 0.1f ? WeatherKind.Klar : r < 0.5f ? WeatherKind.Bewoelkt : r < 0.8f ? WeatherKind.Regen : WeatherKind.Gewitter;
                case WeatherKind.Gewitter:
                    return r < 0.6f ? WeatherKind.Regen : WeatherKind.Bewoelkt;
                default:
                    return r < 0.5f ? WeatherKind.Klar : WeatherKind.Bewoelkt;
            }
        }

        float TargetLight()
        {
            switch (Current)
            {
                case WeatherKind.Klar: return 1f;
                case WeatherKind.Bewoelkt: return 0.75f;
                case WeatherKind.Regen: return 0.55f;
                case WeatherKind.Gewitter: return 0.4f;
                default: return 0.7f;
            }
        }

        float TargetFog()
        {
            switch (Current)
            {
                case WeatherKind.Klar: return 0.0025f;
                case WeatherKind.Bewoelkt: return 0.004f;
                case WeatherKind.Regen: return 0.009f;
                case WeatherKind.Gewitter: return 0.012f;
                default: return 0.03f;
            }
        }

        void Update()
        {
            if (!World.Ready) return;
            float dt = Time.deltaTime;
            float night = World.Clock != null && World.Clock.IsNight ? 1.4f : 1f;
            LightMult = Mathf.MoveTowards(LightMult, TargetLight(), dt * 0.1f);
            fogDensity = Mathf.MoveTowards(fogDensity, TargetFog() * night, dt * 0.002f);
            RenderSettings.fogDensity = fogDensity;

            if (Current == WeatherKind.Gewitter && Time.time > nextThunder)
            {
                nextThunder = Time.time + Random.Range(6f, 18f);
                flash = 1f;
            }
            flash = Mathf.MoveTowards(flash, 0f, dt * 4f);

            if (rain != null)
            {
                var cam = World.Rig != null ? World.Rig.transform : null;
                if (cam != null) rain.transform.position = cam.position + Vector3.up * 14f + cam.forward * 8f;
                var emission = rain.emission;
                float rate = Current == WeatherKind.Regen ? 1500f : Current == WeatherKind.Gewitter ? 3000f : 0f;
                if (World.Tower != null && World.Tower.InTower) rate = 0f;
                emission.rateOverTime = rate;
            }
        }
    }
}
