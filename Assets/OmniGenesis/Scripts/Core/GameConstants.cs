namespace OmniGenesis
{
    // Zentrale Spielregeln. Alles, was das Edikt in Zahlen festlegt, steht hier an einer Stelle.
    public static class GameConstants
    {
        public const string Title = "Omni Genesis";
        public const string Motto = "Ein Kontinent. Ein Thron. Ein Gesetz.";

        public const string MainMenuScene = "Hauptmenue";
        public const string WorldScene = "Welt";

        // Stufen
        public const int MaxLevel = 100;
        public const int SecondClassLevel = 50;
        public const int StatPointsPerLevel = 3;

        // Der Kaiser. Ein Prätendent muss mindestens seine Stufe erreichen (Art. 129 Nr. 3: "würdig an Kraft").
        public const string EmperorName = "Mortem Underwood";
        public const string EmperorTitle = "Oberster Kaiser des Großen Reiches Omni Genesis";
        public const int EmperorLevel = 100;
        public const string JudgeName = "Richter Ataklas";

        // Zeit: 1 Spielstunde = 60 Sekunden, 1 Spieltag = 24 Minuten.
        public const float SecondsPerGameHour = 60f;
        // 1 Jahr laut Edikt = 30 Spieltage (sonst dauert eine Revision Wochen echter Zeit).
        public const int DaysPerEdictYear = 30;
        public const int RevisionDays = 3;          // Art. 134: höchstens ein Jahr
        public const int CourtDeadlineDays = 3;     // Art. 29: binnen drei Tagen vor Gericht

        // Wirtschaft (Art. 93, 115, 117, 34)
        public const float TradeTax = 0.10f;
        public const float RentTaxShare = 0.10f;
        public const float TowerTreasuryShare = 0.10f;
        public const int GateFee = 25;
        public const int RoomRentPerDay = 30;
        public const int PetitionFee = 1000;
        public const int PermitFee = 200;
        public const int TrialFee = 100;

        // Drei Säulen (Art. 130–133). Laut Edikt reichen 2/3 der Einwohner,
        // in Omni Genesis müssen ALLE Einwohner den Prätendenten respektieren.
        public const float RespectThreshold = 50f;
        public const int PillarTaler = 20000;
        public const int PillarFood = 30;
        public const int PillarWood = 30;
        public const int PillarIron = 15;
        public const float PillarGameStock = 0.5f;
        public const int PillarGuards = 4;
        public const int PillarLaws = 3;
        public const int GuardHireCost = 400;
        public const int GuardUpkeepPerDay = 20;
        public const int MaxGuards = 6;
        public const int TreatyWarningDays = 3;     // Art. 140: Vorwarnung mit Frist

        // Der Turm (Art. 113)
        public const int TowerFloors = 50;

        // Physik: F = m · a. Aus der Kraft (Newton) wird Schaden.
        public const float DamagePerNewton = 0.5f;
        public const float DamagePerJoule = 0.3f;   // Pfeile: Schaden aus kinetischer Energie
        public const float ImpactTime = 0.1f;       // Kontaktzeit Δt für den Impuls J = F · Δt
        public const float KnockbackScale = 8f;     // Stilisierung, damit Rückstoß sichtbar wird

        // Ökosystem: getötete Tiere kehren nie zurück, nur Nachwuchs der Überlebenden.
        public static readonly bool MigrationEnabled = true;   // Zuwanderung, wenn eine Art ausgestorben ist
        public const float MigrationChancePerDay = 0.25f;
    }
}
