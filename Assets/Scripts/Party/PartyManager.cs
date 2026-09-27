using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eferin
{
    public class PartyManager : MonoBehaviour
    {
        public static PartyManager Instance { get; private set; }

        public List<CharacterData> members = new List<CharacterData>();
        public float switchCooldown = 0.8f;

        public int ActiveIndex { get; private set; }
        public CharacterData Active => members[ActiveIndex];

        public event Action<CharacterData> Switched;

        Vector3 respawnPoint;
        float switchCooldownLeft;

        void Awake()
        {
            Instance = this;
            if (members.Count == 0) members = CharacterData.DefaultRoster();
            foreach (var m in members) m.ResetRuntime();
            respawnPoint = transform.position;
        }

        void Update()
        {
            switchCooldownLeft = Mathf.Max(0f, switchCooldownLeft - Time.deltaTime);
            foreach (var m in members) m.skillCooldownLeft = Mathf.Max(0f, m.skillCooldownLeft - Time.deltaTime);

            if (GameState.GameplayBlocked) return;
            if (GameInput.Pressed(GameKey.Slot1)) TrySwitch(0);
            if (GameInput.Pressed(GameKey.Slot2)) TrySwitch(1);
            if (GameInput.Pressed(GameKey.Slot3)) TrySwitch(2);
            if (GameInput.Pressed(GameKey.Slot4)) TrySwitch(3);
            if (GameInput.Pressed(GameKey.Slot5)) TrySwitch(4);
        }

        public bool TrySwitch(int index)
        {
            if (index < 0 || index >= members.Count || index == ActiveIndex) return false;
            if (switchCooldownLeft > 0f || members[index].IsDown) return false;
            ActiveIndex = index;
            switchCooldownLeft = switchCooldown;
            Switched?.Invoke(Active);
            return true;
        }

        public void DamageActive(float amount)
        {
            var c = Active;
            c.hp = Mathf.Max(0f, c.hp - amount);
            GameHud.Instance?.ShowWorldText(transform.position + Vector3.up * 2f, "-" + Mathf.RoundToInt(amount), new Color(1f, 0.42f, 0.42f));
            if (c.IsDown) OnActiveDown();
        }

        public void ShareEnergy(CharacterData source, float amount)
        {
            foreach (var m in members) m.GainEnergy(m == source ? amount : amount * 0.2f);
        }

        void OnActiveDown()
        {
            GameHud.Instance?.ShowBanner(Active.displayName + " не может сражаться", new Color(1f, 0.6f, 0.6f));
            for (int i = 0; i < members.Count; i++)
            {
                if (!members[i].IsDown)
                {
                    ActiveIndex = i;
                    Switched?.Invoke(Active);
                    return;
                }
            }
            RespawnParty();
        }

        void RespawnParty()
        {
            foreach (var m in members) m.ResetRuntime();
            ActiveIndex = 0;
            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.position = respawnPoint;
            if (controller != null) controller.enabled = true;
            GameHud.Instance?.ShowBanner("Отряд отступил к берегу", new Color(1f, 0.85f, 0.6f));
            Switched?.Invoke(Active);
        }
    }
}
