using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OmniGenesis
{
    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count;

        public ItemStack() { }
        public ItemStack(string id, int count) { this.id = id; this.count = count; }
    }

    [Serializable]
    public class CreatureSave
    {
        public string species;
        public Vector3 position;
        public float age;
        public float hunger;
    }

    [Serializable]
    public class FloatEntry
    {
        public string key;
        public float value;
    }

    [Serializable]
    public class ResidentSave
    {
        public string name;
        public float respect;
        public string needItem;
        public int needCount;
    }

    // Alles, was zwischen Hauptmenü, Welt und Spielständen übertragen wird.
    [Serializable]
    public class SaveData
    {
        public int version = 1;

        // Charakter
        public string name = "Reisender";
        public string race = "mensch";
        public string classId = "novize";
        public string secondClassId = "";
        public int level = 1;
        public float exp;
        public int allocStr, allocAgi, allocVit, allocInt;
        public int freePoints;
        public float hp = -1f, aether = -1f;
        public bool hasPosition;
        public Vector3 position;
        public float yaw;
        public int taler = 150;
        public List<ItemStack> inventory = new List<ItemStack>();
        public List<ItemStack> storage = new List<ItemStack>();
        public string weapon = "";
        public string armor = "";
        public int rankIndex = -1;
        public int towerBest;
        public bool permit;
        public bool demoMode;
        public bool firstPerson;
        public string companionSpecies = "";

        // Welt
        public float gameMinutes = 8 * 60f;
        public int weather;
        public float sealStrength = 70f;
        public bool riftGuardianDead;
        public int riftGuardianDeathDay = -100;
        public bool worldInitialized;
        public List<CreatureSave> creatures = new List<CreatureSave>();
        public List<FloatEntry> depletedNodes = new List<FloatEntry>();
        public List<FloatEntry> market = new List<FloatEntry>();
        public List<FloatEntry> treasury = new List<FloatEntry>();
        public List<ResidentSave> residents = new List<ResidentSave>();
        public List<string> announcements = new List<string>();

        // Recht
        public int wanted;
        public bool greatTreason;
        public int lastCrimeDay = -1000;
        public int pendingFine;
        public List<string> record = new List<string>();

        // Miete
        public int rentPaidUntilDay;

        // Autonomie (Weg der Diplomatie)
        public int guardsHired;
        public bool townHallBuilt;
        public List<string> laws = new List<string>();
        public int petitionState;
        public int revisionEndDay;
        public int audienceDay;
        public int audienceHour;
        public int petitionRejectedDay = -1000;
        public string petitionRejectReason = "";
        public string autonomyName = "";
        public int autonomyColor;
        public int treatyWarningUntil = -1;

        // Chroniken der Genesis
        public List<string> chronicles = new List<string>();
    }

    public static class SaveSystem
    {
        static string FilePath { get { return Path.Combine(Application.persistentDataPath, "omnigenesis_spielstand.json"); } }

        public static bool Exists() { return File.Exists(FilePath); }

        public static SaveData Load()
        {
            try
            {
                if (!Exists()) return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Spielstand konnte nicht geladen werden: " + e.Message);
                return null;
            }
        }

        public static void Write(SaveData data)
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Spielstand konnte nicht gespeichert werden: " + e.Message);
            }
        }

        public static void Delete()
        {
            if (Exists()) File.Delete(FilePath);
        }
    }

    // Übergabe vom Hauptmenü an die Welt-Szene.
    public static class Session
    {
        public static SaveData Current;

        public static SaveData GetOrCreate()
        {
            if (Current != null) return Current;
            Current = SaveSystem.Load();
            if (Current == null)
            {
                // Welt-Szene direkt im Editor gestartet: Testcharakter mit Demo-Modus.
                Current = new SaveData { name = "Testheld", demoMode = true, weapon = "holzknueppel", armor = "stoffkleidung" };
                Current.inventory.Add(new ItemStack("heiltrank", 3));
                Current.inventory.Add(new ItemStack("gebratenes_fleisch", 4));
            }
            return Current;
        }
    }
}
