using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.ECS.Matchmaking;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlayerConnectionSystem : ISystem
    {
        private double _nextLogTime;

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.Time.ElapsedTime < _nextLogTime) return;
            _nextLogTime = SystemAPI.Time.ElapsedTime + 10.0;

            var slots = MatchStateRegistry.Slots;
            if (slots == null) return;

            foreach (var kvp in slots)
            {
                var slot = kvp.Value;
                if (!slot.IsActive) continue;

                int team1Connected = 0;
                int team2Connected = 0;
                var slotIdx = slot.SlotIndex;

                foreach (var (match, team) in SystemAPI.Query<RefRO<MatchComponent>, RefRO<TeamComponent>>())
                {
                    if (match.ValueRO.MatchId == slotIdx && team.ValueRO.Value == 1)
                        team1Connected++;
                    else if (match.ValueRO.MatchId == slotIdx && team.ValueRO.Value == 2)
                        team2Connected++;
                }

                Debug.Log($"[PlayerConnectionSystem] Slot {slot.SlotIndex} | Match {slot.MatchId} | " +
                          $"Team1: {team1Connected} alive ({slot.Team1LifePool} lives) | " +
                          $"Team2: {team2Connected} alive ({slot.Team2LifePool} lives)");
            }
        }
    }
}
