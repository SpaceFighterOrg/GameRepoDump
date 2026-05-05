using Assets.Scripts.ECS.Components.Spawners;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.ECS.Matchmaking;
using Assets.Scripts.Networking.RPCs;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct GoInGameServerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            MatchStateRegistry.Init();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var spawnerBySlot = new NativeHashMap<byte, Entity>(4, Allocator.Temp);
            foreach (var (spawnerComp, spawnerEntity) in SystemAPI
                .Query<RefRO<AsteroidSpawnerComponent>>()
                .WithNone<AsteroidSpawnReadyTag>()
                .WithEntityAccess())
            {
                spawnerBySlot[spawnerComp.ValueRO.MatchSlotIndex] = spawnerEntity;
            }

            foreach (var (rpc, receiveRpc, entity) in SystemAPI
                .Query<RefRO<GoInGameRpc>, RefRO<ReceiveRpcCommandRequest>>()
                .WithEntityAccess())
            {
                var sourceConnection = receiveRpc.ValueRO.SourceConnection;

                if (!state.EntityManager.Exists(sourceConnection) ||
                    !state.EntityManager.HasComponent<NetworkId>(sourceConnection))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (state.EntityManager.HasComponent<NetworkStreamInGame>(sourceConnection))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var networkId = state.EntityManager.GetComponentData<NetworkId>(sourceConnection);

                Debug.Log($"[GoInGame] Accepted connection networkId={networkId.Value}, slot={rpc.ValueRO.MatchSlotIndex}.");
                ecb.AddComponent<NetworkStreamInGame>(sourceConnection);
                ecb.AddComponent(sourceConnection, new ConnectionState { NetworkId = networkId.Value });
                ecb.DestroyEntity(entity);

                var info = new PlayerMatchInfo
                {
                    Team = rpc.ValueRO.Team,
                    AttackingPlanetId = rpc.ValueRO.AttackingPlanetId,
                    BackendPlayerId = rpc.ValueRO.BackendPlayerId,
                    FactionId = rpc.ValueRO.FactionId,
                    Username = rpc.ValueRO.Username.ToString()
                };

                var slotIndex = rpc.ValueRO.MatchSlotIndex;
                var slot = MatchStateRegistry.GetSlot(slotIndex);
                bool isFirstActivation = slot != null && !slot.IsActive;

                MatchStateRegistry.RegisterPlayer(
                    slotIndex,
                    rpc.ValueRO.MatchId.ToString(),
                    (uint)networkId.Value,
                    info
                );

                if (isFirstActivation && spawnerBySlot.TryGetValue(slotIndex, out var spawnerEntity))
                    ecb.AddComponent<AsteroidSpawnReadyTag>(spawnerEntity);

                var updatedSlot = MatchStateRegistry.GetSlot(slotIndex);
                if (updatedSlot != null)
                {
                    var initRpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(initRpcEntity, new LiveLostRPC
                    {
                        TeamThatLost = 0,
                        Team1LivesRemaining = updatedSlot.Team1LifePool,
                        Team2LivesRemaining = updatedSlot.Team2LifePool
                    });
                    ecb.AddComponent(initRpcEntity, new SendRpcCommandRequest());
                }
            }

            spawnerBySlot.Dispose();
            ecb.Playback(state.EntityManager);
        }
    }
}
