using UnityEngine;

namespace OmniGenesis
{
    // Bett im gemieteten Zimmer: schlafen bis 6:00, volle Heilung, Speichern.
    public class Bed : Interactable
    {
        public override string Prompt { get { return Rented ? "Schlafen bis 6:00 Uhr" : "Bett (nur mit gemietetem Zimmer)"; } }

        static bool Rented { get { return World.Data != null && World.Data.rentPaidUntilDay >= World.Day; } }

        public override void Interact(PlayerCharacter player)
        {
            if (!Rented)
            {
                Notify.System("Miete zuerst ein Zimmer beim Vermieter vor dem Haus (Art. 116).");
                return;
            }
            if (World.Law != null && World.Law.Wanted > 0)
            {
                Notify.System("Gesuchte finden keinen Schlaf. Stelle dich dem Gericht (G).");
                return;
            }
            World.Clock.SkipTo(6f);
            player.health.hp = player.health.maxHp;
            player.aether = player.MaxAether;
            WorldBootstrap.SaveGame();
            Notify.System("Du hast geschlafen. " + World.Clock.Stamp() + ". Spiel gespeichert.");
        }
    }

    public class StorageChest : Interactable
    {
        public override string Prompt { get { return "Truhe öffnen"; } }

        public override void Interact(PlayerCharacter player)
        {
            if (World.Data == null || World.Data.rentPaidUntilDay < World.Day)
            {
                Notify.System("Die Truhe gehört zum Mietzimmer. Miete zuerst ein Zimmer.");
                return;
            }
            World.Ui.OpenDialogue(new StorageDialogue());
        }
    }

    public class Anvil : Interactable
    {
        public override string Prompt { get { return "Schmieden (Amboss)"; } }

        public override void Interact(PlayerCharacter player)
        {
            World.Ui.OpenDialogue(new CraftingDialogue(true));
        }
    }

    // Großes Tor: Reisen gegen die einheitliche Gebühr des Throns (Art. 34) und Verkündungen (Art. 54).
    public class GreatGate : Interactable
    {
        public static readonly System.Collections.Generic.List<GreatGate> Gates = new System.Collections.Generic.List<GreatGate>();

        public string gateName;

        protected override void OnEnable() { base.OnEnable(); Gates.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Gates.Remove(this); }

        public override string Prompt { get { return "Großes Tor " + gateName; } }

        public override void Interact(PlayerCharacter player)
        {
            World.Ui.OpenDialogue(new GateDialogue(this));
        }
    }

    public class TownHallSite : Interactable
    {
        public override string Prompt
        {
            get
            {
                if (World.Autonomy != null && World.Autonomy.townHall) return "Rathaus von Eichenhain";
                return "Bauplatz: Rathaus errichten";
            }
        }

        public override void Interact(PlayerCharacter player)
        {
            World.Ui.OpenDialogue(new TownHallDialogue());
        }
    }
}
