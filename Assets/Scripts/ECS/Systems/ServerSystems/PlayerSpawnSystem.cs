using Assets.Scripts;
using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Spawners;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.ECS.Matchmaking;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlayerSpawnSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<PlayerShipSpawnerComponent>(out var spawner))
            {
                Debug.LogWarning("[PlayerSpawn] PlayerShipSpawnerComponent singleton not found — skipping.");
                return;
            }

            if (SystemAPI.TryGetSingletonRW<GhostRelevancy>(out var ghostRelevancy))
                ghostRelevancy.ValueRW.GhostRelevancyMode = GhostRelevancyMode.Disabled;

            float3 team1Center = float3.zero;
            float team1Radius = 0f;
            float3 team2Center = float3.zero;
            float team2Radius = 0f;

            foreach (var (sp, lt, team) in SystemAPI.Query<RefRO<TeamSpawnPointComponent>, RefRO<LocalTransform>, RefRO<TeamComponent>>())
            {
                if(team.ValueRO.Value == 1)
                {
                    team1Center = lt.ValueRO.Position;
                    team1Radius = sp.ValueRO.Radius;
                }
                else if (team.ValueRO.Value == 2)
                {
                    team2Center = lt.ValueRO.Position;
                    team2Radius = sp.ValueRO.Radius;
                }
            }

            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                .WithAll<NetworkStreamInGame>()
                .WithNone<ShipSpawnedTag>()
                .WithEntityAccess())
            {
                var netId = (uint)networkId.ValueRO.Value;
                var ship = ecb.Instantiate(spawner.Prefab);

                Debug.Log($"[PlayerSpawn] Spawning ship for networkId={netId}, prefab={spawner.Prefab}.");

                ecb.AddComponent<ShipSpawnedTag>(entity);
                ecb.SetComponent(entity, new CommandTarget { targetEntity = ship });
                ecb.SetComponent(ship, new GhostOwner { NetworkId = networkId.ValueRO.Value });

                var playerData = MatchStateRegistry.GetPlayerInfo(netId);
                if (playerData.HasValue)
                {
                    var (slotIndex, info) = playerData.Value;
                    ecb.AddComponent(ship, new MatchComponent { MatchId = slotIndex });
                    ecb.AddComponent(ship, new PlayerConnectionCleanup
                    {
                        NetworkId = netId,
                        MatchSlotIndex = slotIndex,
                        Team = info.Team
                    });

                    float3 center;
                    quaternion rotation;
                    float radius;
                    if (info.Team == 1)
                    {
                        ecb.SetComponent(ship, new TeamComponent { Value = 1 });
                        center = team1Center;
                        radius = team1Radius;
                        rotation = quaternion.RotateY(math.radians(180f));
                        
                    }
                    else
                    {
                        ecb.SetComponent(ship, new TeamComponent { Value = 2 });
                        center = team2Center;
                        radius = team2Radius;
                        rotation = quaternion.identity;
                    }

                    ecb.SetComponent(ship, new UsernameComponent { Username = new FixedString128Bytes(info.Username ?? string.Empty) });

                    var rng = new Unity.Mathematics.Random(netId * 2654435761u + 1u);
                    float3 spawnPos = center + rng.NextFloat3Direction() * rng.NextFloat(0f, radius);
                    ecb.SetComponent(ship, LocalTransform.FromPositionRotation(spawnPos, rotation));
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
