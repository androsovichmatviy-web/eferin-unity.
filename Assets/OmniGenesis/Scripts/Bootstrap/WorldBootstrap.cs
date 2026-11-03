using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Einstiegspunkt der Szene "Welt": baut Land, Städte, Wesen und Systeme Schritt für Schritt
    // (der Ladebildschirm zeigt den Fortschritt in Prozent).
    public class WorldBootstrap : MonoBehaviour
    {
        public static float Progress;
        public static string Status = "";

        public static Vector3 LazarettSpawn { get { return Places.LazarettSpawn; } }

        void Start()
        {
            Time.timeScale = 1f;
            World.Reset();
            Notify.Clear();
            Places.Clear();
            Progress = 0f;
            if (LoadingScreen.Instance == null) LoadingScreen.Overlay();
            StartCoroutine(Generate());
        }

        static void Step(float p, string s)
        {
            Progress = p;
            Status = s;
        }

        IEnumerator Generate()
        {
            var d = Session.GetOrCreate();
            World.Data = d;
            Step(0.02f, "Erwecke den Thron der Genesis…");
            yield return null;

            // Kamera und Licht
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            // Ohne Skybox-Material färbt die Uhr den Himmel selbst (Nebelfarbe je nach Tageszeit).
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.farClipPlane = 900f;
            cam.fieldOfView = 65f;

            var sun = new GameObject("Sonne").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            var moon = new GameObject("Mond").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.shadows = LightShadows.None;
            moon.color = new Color(0.55f, 0.65f, 1f);

            // Systeme
            var sys = new GameObject("Systeme");
            var clock = sys.AddComponent<WorldClock>();
            clock.sun = sun;
            clock.moon = moon;
            World.Clock = clock;
            World.Weather = sys.AddComponent<Weather>();
            World.Weather.Init(d.weather, clock);
            clock.Init(d.gameMinutes);
            World.Treasury = new Treasury();
            World.Treasury.Load(d);
            World.Market = sys.AddComponent<Market>();
            World.Law = sys.AddComponent<Law>();
            World.Eco = sys.AddComponent<Ecosystem>();
            World.Residents = sys.AddComponent<Residents>();
            World.Autonomy = sys.AddComponent<Autonomy>();
            World.Quests = sys.AddComponent<Quests>();
            World.Trial = sys.AddComponent<RankTrial>();
            World.Nodes = sys.AddComponent<ResourceNodes>();
            World.Ui = sys.AddComponent<UiWindows>();
            World.Hud = sys.AddComponent<Hud>();
            foreach (var a in d.announcements) Notify.Announcements.Add(a);

            // Bodentextur
            const int size = 512;
            var px = new Color32[size * size];
            for (int y0 = 0; y0 < size; y0 += 32)
            {
                Land.FillTextureRows(px, size, y0, Mathf.Min(size, y0 + 32));
                Step(0.04f + 0.24f * y0 / size, "Zeichne das Land…");
                yield return null;
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 4;
            tex.Apply();
            World.Ui.SetMapTexture(tex);

            // Gelände
            var mat = MaterialFactory.Textured(tex, 0.05f);
            var landRoot = new GameObject("Kontinent").transform;
            int built = 0;
            for (int cz = 0; cz < Land.Chunks; cz++)
            {
                for (int cx = 0; cx < Land.Chunks; cx++)
                {
                    Land.BuildChunk(cx, cz, mat, landRoot);
                    built++;
                    Step(0.28f + 0.2f * built / (Land.Chunks * Land.Chunks), "Forme Täler und Höhen…");
                    yield return null;
                }
            }
            BuildBounds(landRoot);

            Step(0.5f, "Errichte Grad Genesis…");
            yield return null;
            WorldBuilder.BuildCity(landRoot);
            Step(0.56f, "Errichte das Gildenhaus und die Arena…");
            yield return null;
            WorldBuilder.BuildOutskirts(landRoot);
            Step(0.6f, "Gründe Eichenhain…");
            yield return null;
            WorldBuilder.BuildVillage(landRoot);
            Step(0.64f, "Versiegle den Riss…");
            yield return null;
            World.Rift = WorldBuilder.BuildForbidden(landRoot);
            WorldBuilder.BuildStoryPlaces(landRoot);
            Step(0.68f, "Pflanze die Wälder…");
            yield return null;
            WorldBuilder.PlantVegetation(landRoot, World.Nodes);

            World.Nodes.Init(clock);
            World.Nodes.Load(d);
            World.Market.Init(d, clock);
            World.Law.Load(d);
            World.Rift.Init(d, clock);

            Step(0.76f, "Rufe die Bürger des Reiches…");
            yield return null;
            SpawnNpcs();

            Step(0.82f, "Der Reisende erwacht…");
            yield return null;
            SpawnPlayer(d, cam);

            Step(0.87f, "Eichenhain erwacht…");
            yield return null;
            World.Residents.Init(d, clock);
            World.Autonomy.Init(d, clock);

            Step(0.92f, "Siedle die Wildtiere an…");
            yield return null;
            World.Eco.Init(d, clock);
            RestoreCompanion(d);
            Step(0.96f, "Die Geschichte beginnt…");
            yield return null;
            World.Quests.Init(d, clock);

            clock.DayStarted += day => SaveGame();
            bool firstVisit = !d.worldInitialized;
            d.worldInitialized = true;
            Step(1f, "Fertig");
            yield return null;
            World.Ready = true;

            if (firstVisit)
            {
                Notify.System("Willkommen in Grad Genesis, " + World.Player.charName + ". Deine Aufträge findest du unter L, die Gesetze dieser Welt im Edikt (J).");
                Notify.System("Ziel nach Art. 127: eine eigene Autonomie. Werde so stark wie der Kaiser (Stufe 100) und gewinne den Respekt aller Einwohner von Eichenhain (P).");
                Notify.Announce("Der Thron der Genesis grüßt alle Reisenden. Ein Kontinent. Ein Thron. Ein Gesetz.");
            }
            else Notify.System("Willkommen zurück, " + World.Player.charName + ". " + World.Clock.Stamp() + ".");
        }

        static void BuildBounds(Transform root)
        {
            float e = WorldLayout.Half - 2f;
            for (int i = 0; i < 4; i++)
            {
                var b = new GameObject("Weltgrenze");
                b.transform.SetParent(root, false);
                var box = b.AddComponent<BoxCollider>();
                bool xAxis = i < 2;
                float s = i % 2 == 0 ? 1f : -1f;
                b.transform.position = xAxis ? new Vector3(s * e, 40f, 0f) : new Vector3(0f, 40f, s * e);
                box.size = xAxis ? new Vector3(2f, 120f, WorldLayout.Half * 2f) : new Vector3(WorldLayout.Half * 2f, 120f, 2f);
            }
        }

        void SpawnNpcs()
        {
            Color gold = new Color(0.95f, 0.78f, 0.3f);
            Color azure = new Color(0.08f, 0.14f, 0.35f);
            Npc.Spawn(NpcRole.Kaiser, GameConstants.EmperorName, "Oberster Kaiser", Places.EmperorPos, Places.EmperorYaw, new Color(0.08f, 0.08f, 0.14f), gold).radius = 4.5f;
            Npc.Spawn(NpcRole.Richter, "Ataklas", "Oberster Arbiter des Throns", Places.JudgePos, Places.JudgeYaw, new Color(0.35f, 0.35f, 0.38f), new Color(0.8f, 0.8f, 0.85f)).radius = 4f;
            Npc.Spawn(NpcRole.Haendler, "Mirela", "Händlerin", Places.MerchantA, 90f, new Color(0.6f, 0.2f, 0.2f), new Color(0.3f, 0.2f, 0.15f));
            Npc.Spawn(NpcRole.Haendler, "Oskar", "Händler", Places.MerchantB, -90f, new Color(0.2f, 0.4f, 0.6f), new Color(0.3f, 0.2f, 0.15f));
            Npc.Spawn(NpcRole.Schmied, "Gundram", "Schmied", Places.SmithPos, -110f, new Color(0.35f, 0.25f, 0.18f), new Color(0.2f, 0.2f, 0.2f));
            Npc.Spawn(NpcRole.Vermieter, "Henrike", "Vermieterin", Places.LandlordPos, 90f, new Color(0.5f, 0.45f, 0.3f), new Color(0.3f, 0.25f, 0.2f));
            Npc.Spawn(NpcRole.Heiler, "Schwester Alwine", "Heilerin des Lazaretts", Places.HealerPos, -90f, new Color(0.95f, 0.95f, 0.95f), new Color(0.7f, 0.2f, 0.2f));
            Npc.Spawn(NpcRole.Pruefungsmeister, "Brakka", "Prüfungsmeisterin", Places.TrialMasterPos, 90f, new Color(0.3f, 0.35f, 0.5f), gold);
            Npc.Spawn(NpcRole.Gildenmeister, "Runa", "Gildenmeisterin der Abenteurer", Places.GuildMasterPos, -90f, new Color(0.3f, 0.45f, 0.3f), gold);

            string[] legion = { "Varus", "Tilda", "Marek", "Odila", "Konrad", "Sabin" };
            for (int i = 0; i < legion.Length; i++)
            {
                var route = new List<Vector3>();
                for (int k = 0; k < Places.CityPatrol.Count; k++) route.Add(Places.CityPatrol[(k + i * 2) % Places.CityPatrol.Count]);
                var g = Npc.Spawn(NpcRole.Wache, "Legionär " + legion[i], "Kaiserliche Legion", route[0], 0f, azure, new Color(0.7f, 0.72f, 0.75f));
                g.route = route;
            }

            string[] citizens = { "Edda", "Florian", "Gisela", "Hagen", "Irmgard", "Jost", "Liesel", "Ruprecht" };
            for (int i = 0; i < citizens.Length; i++)
            {
                var route = new List<Vector3>();
                for (int k = 0; k < 4; k++) route.Add(Places.CityWalk[(i + k * 3) % Places.CityWalk.Count]);
                var c = Npc.Spawn(NpcRole.Buerger, citizens[i], "Bürger von Grad Genesis", route[0], 0f, Color.HSVToRGB((i * 0.17f) % 1f, 0.4f, 0.6f), new Color(0.3f, 0.25f, 0.2f));
                c.route = route;
            }

            Npc.Spawn(NpcRole.Ordensmagier, "Magistra Velde", "Orden der Kampfmagier", Places.BorderPostL, 180f, new Color(0.35f, 0.15f, 0.5f), gold);
            Npc.Spawn(NpcRole.Ordensmagier, "Magister Aurel", "Orden der Kampfmagier", Places.BorderPostR, 180f, new Color(0.35f, 0.15f, 0.5f), gold);

            Npc.Spawn(NpcRole.Kraemer, "Wendelin", "Krämer von Eichenhain", Places.VillageTraderPos, 90f, new Color(0.45f, 0.4f, 0.25f), new Color(0.3f, 0.25f, 0.2f));
            Npc.Spawn(NpcRole.Soeldnerfuehrer, "Hedda", "Söldnerführerin", Places.MercenaryPos, -90f, new Color(0.3f, 0.32f, 0.28f), new Color(0.5f, 0.5f, 0.52f));
        }

        void SpawnPlayer(SaveData d, Camera cam)
        {
            var race = Races.Get(d.race);
            var go = new GameObject("Spieler");
            Vector3 pos = d.hasPosition && d.position.x < 1000f ? d.position : Places.CityGate + new Vector3(0f, 0.3f, -7f);
            go.transform.position = pos + Vector3.up * 0.2f;
            go.transform.rotation = Quaternion.Euler(0f, d.hasPosition ? d.yaw : 180f, 0f);

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f * race.heightScale;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f * race.heightScale, 0f);
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 50f;

            var pc = go.AddComponent<PlayerCharacter>();
            World.Player = pc;
            pc.Init(d);
            pc.motor = go.AddComponent<PlayerMotor>();
            pc.combat = go.AddComponent<PlayerCombat>();
            go.AddComponent<PlayerInteractor>();
            pc.visual = HumanoidVisual.Build(go.transform, pc.cls.color, new Color(0.2f, 0.18f, 0.16f), race.skin, race.heightScale);
            pc.visual.SetWeapon(pc.Weapon);

            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            World.Rig = rig;
            rig.Init(go.transform, d.firstPerson, 1.62f * race.heightScale);
        }

        void RestoreCompanion(SaveData d)
        {
            if (string.IsNullOrEmpty(d.companionSpecies)) return;
            var def = Species.Get(d.companionSpecies);
            if (def == null) return;
            var c = Creature.Spawn(def, World.Player.transform.position + World.Player.transform.right * 2f, 48f);
            c.Tame(true);
        }

        void Update()
        {
            if (!World.Ready || World.Player == null || !World.Player.demo || World.GameplayBlocked) return;
            var pc = World.Player;
            if (GameInput.Pressed(Act.Demo1))
            {
                for (int i = 0; i < 5 && pc.level < GameConstants.MaxLevel; i++)
                    pc.GainExp((PlayerCharacter.ExpToNext(pc.level) - pc.exp) / (1f + pc.race.expBonus) + 1f);
            }
            if (GameInput.Pressed(Act.Demo2)) { pc.AddTaler(5000); Notify.System("Demo: +5000 Taler."); }
            if (GameInput.Pressed(Act.Demo3)) { World.Clock.Advance(360f); Notify.System("Demo: sechs Stunden vergehen. " + World.Clock.Stamp()); }
            if (GameInput.Pressed(Act.Demo4)) { World.Residents.ChangeAll(25f); Notify.System("Demo: Respekt aller Einwohner +25."); }
            if (GameInput.Pressed(Act.Demo5)) { World.Rift.Weaken(30f); Notify.System("Demo: Siegel −30 %."); }
        }

        void OnApplicationQuit()
        {
            SaveGame();
        }

        public static void SaveGame()
        {
            if (!World.Ready || World.Data == null || World.Player == null) return;
            var d = World.Data;
            var pc = World.Player;
            pc.Save(d);
            bool away = World.Trial.Active;
            d.hasPosition = !away;
            d.position = pc.transform.position;
            d.yaw = pc.transform.eulerAngles.y;
            d.firstPerson = World.Rig != null && World.Rig.FirstPerson;
            d.gameMinutes = World.Clock.minutes;
            d.weather = (int)World.Weather.Current;
            World.Eco.Save(d);
            World.Nodes.Save(d);
            World.Market.Save(d);
            World.Treasury.Save(d);
            World.Residents.Save(d);
            World.Law.Save(d);
            World.Autonomy.Save(d);
            World.Quests.Save(d);
            World.Rift.Save(d);
            d.announcements = new List<string>(Notify.Announcements);
            SaveSystem.Write(d);
        }

        // Bodenhöhe per Strahl nach unten (Gebäudeböden, Felsen), sonst die Höhenfunktion.
        public static float GroundHeight(Vector3 p, Transform ignore = null)
        {
            float fallback = Land.Height(p.x, p.z);
            Vector3 origin = new Vector3(p.x, Mathf.Max(p.y, fallback) + 2.5f, p.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            foreach (var h in hits)
            {
                if (ignore != null && h.transform.root == ignore.root) continue;
                if (h.collider.GetComponentInParent<Health>() != null) continue;
                if (h.point.y > best) best = h.point.y;
            }
            return best > float.NegativeInfinity ? best : fallback;
        }

        public static float GroundHeight(Vector3 p) { return GroundHeight(p, null); }

        public static Vector3 SafeSpawn()
        {
            return Places.CityGate + new Vector3(0f, 0.5f, -7f);
        }
    }
}
