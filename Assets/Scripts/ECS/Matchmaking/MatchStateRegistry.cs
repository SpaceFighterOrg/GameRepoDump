using System;
using System.Collections.Generic;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.ECS.Matchmaking
{
    public static class MatchStateRegistry
    {
        private static Dictionary<byte, MatchSlotState> _slots;
        private static bool _initialized;

        public static IReadOnlyDictionary<byte, MatchSlotState> Slots => _slots;

        public static void Init()
        {
            if (_initialized) return;

            int capacity;
            try
            {
                capacity = int.Parse(EnvironmentConfig.MAX_MATCHES);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MatchStateRegistry] Env config unavailable ({e.Message}), using editor defaults.");
                capacity = 1;
            }

            _slots = new Dictionary<byte, MatchSlotState>(capacity);
            for (byte i = 0; i < capacity; i++)
                _slots[i] = new MatchSlotState { SlotIndex = i };

            _initialized = true;
            Debug.Log($"[MatchStateRegistry] Initialized {capacity} slots.");
        }

        public static MatchSlotState GetSlot(byte slotIndex)
            => _slots != null && _slots.TryGetValue(slotIndex, out var slot) ? slot : null;

        public static (byte slotIndex, PlayerMatchInfo info)? GetPlayerInfo(uint networkId)
        {
            if (_slots == null) return null;
            foreach (var kvp in _slots)
            {
                if (kvp.Value.Players.TryGetValue(networkId, out var info))
                    return (kvp.Key, info);
            }
            return null;
        }

        public static void RegisterPlayer(byte slotIndex, string matchId, uint networkId, PlayerMatchInfo info)
        {
            if (_slots == null || !_slots.TryGetValue(slotIndex, out var slot)) return;

            if (!slot.IsActive)
            {
                slot.IsActive = true;
                slot.MatchId = matchId;
                slot.Team1LifePool = 0;
                slot.Team2LifePool = 0;
                Debug.Log($"[MatchStateRegistry] Slot {slotIndex} activated for match {matchId}");
            }

            slot.Players[networkId] = info;

            if (info.Team == 1)
                slot.Team1LifePool++;
            else
                slot.Team2LifePool++;

            Debug.Log($"[MatchStateRegistry] Player {networkId} registered in slot {slotIndex} as team {info.Team}, " +
                      $"attacking planet {info.AttackingPlanetId} (faction {info.FactionId})");
        }

        // Returns true if the match is over after this death.
        public static bool OnPlayerDied(uint networkId, byte slotIndex, out byte winningTeam, out int winnerRemainingPool)
        {
            winningTeam = 0;
            winnerRemainingPool = 0;

            var slot = GetSlot(slotIndex);
            if (slot == null || !slot.IsActive) return false;
            if (!slot.Players.TryGetValue(networkId, out var info)) return false;

            if (info.Team == 1)
            {
                slot.Team1LifePool = Math.Max(0, slot.Team1LifePool - 1);
                Debug.Log($"[MatchStateRegistry] Slot {slotIndex}: Team1 life pool → {slot.Team1LifePool}");
                if (slot.Team1LifePool == 0)
                {
                    winningTeam = 2;
                    winnerRemainingPool = slot.Team2LifePool;
                    return true;
                }
            }
            else
            {
                slot.Team2LifePool = Math.Max(0, slot.Team2LifePool - 1);
                Debug.Log($"[MatchStateRegistry] Slot {slotIndex}: Team2 life pool → {slot.Team2LifePool}");
                if (slot.Team2LifePool == 0)
                {
                    winningTeam = 1;
                    winnerRemainingPool = slot.Team1LifePool;
                    return true;
                }
            }

            return false;
        }

        public static void CleanupSlot(byte slotIndex)
        {
            var slot = GetSlot(slotIndex);
            if (slot == null) return;

            slot.IsActive = false;
            slot.MatchId = null;
            slot.Team1LifePool = 0;
            slot.Team2LifePool = 0;
            slot.Players.Clear();

            Debug.Log($"[MatchStateRegistry] Slot {slotIndex} cleaned up and ready for reuse");
        }
    }
}
