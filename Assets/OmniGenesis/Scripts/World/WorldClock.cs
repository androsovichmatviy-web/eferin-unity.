using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OmniGenesis
{
    // Tag-/Nacht-Zyklus. 1 Spielstunde = GameConstants.SecondsPerGameHour Sekunden.
    public class WorldClock : MonoBehaviour
    {
        public float minutes;
        public Light sun;
        public Light moon;

        public event Action<int, int> HourPassed;   // (Tag, Stunde)
        public event Action<int> DayStarted;        // um 6:00 Uhr

        long lastHourIndex;

        public int Day { get { return (int)(minutes / 1440f) + 1; } }
        public float HourF { get { return (minutes % 1440f) / 60f; } }
        public int Hour { get { return (int)HourF; } }
        public bool IsNight { get { float h = HourF; return h < 6f || h >= 21f; } }

        public float Daylight
        {
            get
            {
                float ang = (HourF / 24f) * 360f - 90f;
                return Mathf.Clamp01(Mathf.Sin(ang * Mathf.Deg2Rad) * 1.6f + 0.15f);
            }
        }

        public void Init(float startMinutes)
        {
            minutes = startMinutes;
            lastHourIndex = (long)(minutes / 60f);
            UpdateLighting();
        }

        public string Stamp()
        {
            int m = (int)(minutes % 60f);
            return "Tag " + Day + ", " + Hour.ToString("00") + ":" + m.ToString("00");
        }

        public void Advance(float gameMinutes)
        {
            minutes += gameMinutes;
            ProcessHours();
        }

        // Springt bis zur nächsten angegebenen Uhrzeit (z. B. Schlafen bis 6:00).
        public void SkipTo(float hour)
        {
            float now = HourF;
            float delta = hour - now;
            if (delta <= 0f) delta += 24f;
            Advance(delta * 60f);
        }

        void Update()
        {
            if (!World.Ready) return;
            minutes += Time.deltaTime * 60f / GameConstants.SecondsPerGameHour;
            ProcessHours();
            UpdateLighting();
        }

        void ProcessHours()
        {
            long idx = (long)(minutes / 60f);
            while (lastHourIndex < idx)
            {
                lastHourIndex++;
                int day = (int)(lastHourIndex / 24) + 1;
                int hour = (int)(lastHourIndex % 24);
                if (HourPassed != null) HourPassed(day, hour);
                if (hour == 6 && DayStarted != null) DayStarted(day);
            }
        }

        public void UpdateLighting()
        {
            if (sun == null) return;
            float ang = (HourF / 24f) * 360f - 90f;
            float day = Daylight;
            float weatherLight = World.Weather != null ? World.Weather.LightMult : 1f;

            sun.transform.rotation = Quaternion.Euler(ang, 160f, 0f);
            float elev = Mathf.Sin(ang * Mathf.Deg2Rad);
            sun.intensity = Mathf.Lerp(0f, 1.3f, day) * weatherLight + (World.Weather != null ? World.Weather.Flash * 2.5f : 0f);
            sun.color = Color.Lerp(new Color(1f, 0.55f, 0.32f), new Color(1f, 0.96f, 0.9f), Mathf.Clamp01(elev * 2.5f));
            sun.shadows = elev > 0.03f ? LightShadows.Soft : LightShadows.None;

            if (moon != null)
            {
                moon.transform.rotation = Quaternion.Euler(ang + 180f, 200f, 0f);
                moon.intensity = (1f - day) * 0.28f * Mathf.Lerp(0.5f, 1f, weatherLight);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            Color skyDay = new Color(0.55f, 0.65f, 0.8f), skyNight = new Color(0.06f, 0.08f, 0.16f);
            Color eqDay = new Color(0.45f, 0.48f, 0.45f), eqNight = new Color(0.05f, 0.06f, 0.1f);
            Color grDay = new Color(0.25f, 0.24f, 0.2f), grNight = new Color(0.03f, 0.03f, 0.05f);
            float amb = Mathf.Lerp(0.75f, 1f, weatherLight);
            RenderSettings.ambientSkyColor = Color.Lerp(skyNight, skyDay, day) * amb;
            RenderSettings.ambientEquatorColor = Color.Lerp(eqNight, eqDay, day) * amb;
            RenderSettings.ambientGroundColor = Color.Lerp(grNight, grDay, day);

            Color fogDay = new Color(0.68f, 0.74f, 0.82f), fogNight = new Color(0.04f, 0.05f, 0.09f);
            Color fog = Color.Lerp(fogNight, fogDay, day);
            if (World.Weather != null) fog = Color.Lerp(fog, fog * 0.7f + new Color(0.15f, 0.15f, 0.17f), 1f - weatherLight);
            RenderSettings.fogColor = fog;

            var cam = World.Rig != null ? World.Rig.Cam : Camera.main;
            if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor) cam.backgroundColor = fog;
        }
    }
}
