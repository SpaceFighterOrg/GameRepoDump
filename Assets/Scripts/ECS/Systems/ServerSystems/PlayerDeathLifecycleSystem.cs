using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.ECS.Matchmaking;
using Assets.Scripts.Networking.RPCs;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerDeathLifecycleSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var connectionByNetId = new NativeHashMap<uint, Entity>(16, Allocator.Temp);
            foreach (var (networkId, connEntity) in SystemAPI.Query<RefRO<NetworkId>>()
                .WithAll<NetworkStreamInGame>()
                .WithEntityAccess())
            {
                connectionByNetId[(uint)networkId.ValueRO.Value] = connEntity;
            }

            foreach (var (cleanup, entity) in SystemAPI.Query<RefRO<PlayerConnectionCleanup>>()
                .WithAll<DeadTag, PlayerShipTag>()
                .WithNone<DeathProcessedTag>()
                .WithEntityAccess())
            {
                var netId = cleanup.ValueRO.NetworkId;
                var slotIndex = cleanup.ValueRO.MatchSlotIndex;

                bool matchOver = MatchStateRegistry.OnPlayerDied(
                    netId, slotIndex, out byte winningTeam, out int remainingPool);

                ecb.AddComponent<DeathProcessedTag>(entity);

                var slot = MatchStateRegistry.GetSlot(slotIndex);
                if (slot != null)
                {
                    var liveLostRpc = new LiveLostRPC
                    {
                        TeamThatLost = cleanup.ValueRO.Team,
                        Team1LivesRemaining = slot.Team1LifePool,
                        Team2LivesRemaining = slot.Team2LifePool
                    };

                    foreach (var (playerId, playerInfo) in slot.Players)
                    {
                        if (!connectionByNetId.TryGetValue(playerId, out var conn)) continue;

                        var liveLostEntity = ecb.CreateEntity();
                        ecb.AddComponent(liveLostEntity, liveLostRpc);
                        ecb.AddComponent(liveLostEntity, new SendRpcCommandRequest { TargetConnection = conn });

                        if (matchOver)
                        {
                            var gameOverEntity = ecb.CreateEntity();
                            ecb.AddComponent(gameOverEntity, new GameOverRPC
                            {
                                WinningTeam = winningTeam,
                                ContributionAmount = playerInfo.Team == winningTeam ? remainingPool : 0,
                                AttackingPlanetId = playerInfo.AttackingPlanetId,
                                DefendingPlanetId = playerInfo.DefendingPlanetId,
                                FactionId = playerInfo.FactionId
                            });
                            ecb.AddComponent(gameOverEntity, new SendRpcCommandRequest { TargetConnection = conn });
                        }
                    }
                }

                if (matchOver)
                    MatchLifecycleSystem.HandleMatchOver(slotIndex, winningTeam, remainingPool);
            }

            connectionByNetId.Dispose();
            ecb.Playback(state.EntityManager);
        }
    }
}
