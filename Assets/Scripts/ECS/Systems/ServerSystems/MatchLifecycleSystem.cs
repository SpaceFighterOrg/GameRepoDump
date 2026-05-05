using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.ECS.Matchmaking;
using Assets.Scripts.Networking.RPCs;
using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct MatchLifecycleSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var connectionByNetId = new NativeHashMap<uint, Entity>(16, Allocator.Temp);
            foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                .WithAll<NetworkStreamInGame>()
                .WithEntityAccess())
            {
                connectionByNetId[(uint)networkId.ValueRO.Value] = entity;
            }

            foreach (var (cleanup, entity) in SystemAPI.Query<RefRO<PlayerConnectionCleanup>>()
                .WithNone<PlayerShipTag>()
                .WithEntityAccess())
            {
                var netId = cleanup.ValueRO.NetworkId;
                var slotIndex = cleanup.ValueRO.MatchSlotIndex;

                bool matchOver = MatchStateRegistry.OnPlayerDied(
                    netId, slotIndex, out byte winningTeam, out int remainingPool);

                ecb.RemoveComponent<PlayerConnectionCleanup>(entity);

                var slot = MatchStateRegistry.GetSlot(slotIndex);
                if (slot != null)
                {
                    var rpcData = new LiveLostRPC
                    {
                        TeamThatLost = cleanup.ValueRO.Team,
                        Team1LivesRemaining = slot.Team1LifePool,
                        Team2LivesRemaining = slot.Team2LifePool
                    };

                    foreach (var (playerId, _) in slot.Players)
                    {
                        if (!connectionByNetId.TryGetValue(playerId, out var conn)) continue;
                        var rpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(rpcEntity, rpcData);
                        ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = conn });
                    }
                }

                if (matchOver)
                {
                    if (slot != null)
                    {
                        foreach (var (playerId, playerInfo) in slot.Players)
                        {
                            if (!connectionByNetId.TryGetValue(playerId, out var conn)) continue;
                            var rpcEntity = ecb.CreateEntity();

                            _ = ApiService.Api.BattleContributionAsync(new()
                            {
                                Amount = playerInfo.Team == winningTeam ? remainingPool : 0,
                                AttackingPlanetId = playerInfo.AttackingPlanetId,
                                DefendingPlanetId = playerInfo.DefendingPlanetId,
                                FactionId = playerInfo.FactionId
                            });

                            ecb.AddComponent(rpcEntity, new GameOverRPC
                            {
                                WinningTeam = winningTeam,
                                ContributionAmount = playerInfo.Team == winningTeam ? remainingPool : 0,
                                AttackingPlanetId = playerInfo.AttackingPlanetId,
                                DefendingPlanetId = playerInfo.DefendingPlanetId,
                                FactionId = playerInfo.FactionId
                            });
                            ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = conn });
                        }
                    }
                    HandleMatchOver(slotIndex, winningTeam, remainingPool);
                }
            }

            connectionByNetId.Dispose();
            ecb.Playback(state.EntityManager);
        }

        public static void HandleMatchOver(byte slotIndex, byte winningTeam, int winnerRemainingPool)
        {
            var slot = MatchStateRegistry.GetSlot(slotIndex);
            if (slot == null) return;

            Debug.Log($"[MatchLifecycle] Match {slot.MatchId} over — " +
                      $"team {winningTeam} wins with {winnerRemainingPool} lives remaining.");

            _ = ReportAndCleanupAsync(slot, winningTeam, winnerRemainingPool);
        }

        private static async Task ReportAndCleanupAsync(
            MatchSlotState slot,
            byte winningTeam,
            int controlPoints)
        {
            try
            {
                await ApiService.GameServer.MatchesDELETEAsync(slot.MatchId).ConfigureAwait(false);
                foreach (var item in slot.Players)
                {
                    int eloChange = 0;
                    int levelChange = 5;
                    
                    if(item.Value.Team == winningTeam)
                    {
                        eloChange += 5;
                        levelChange += 5;
                        await ApiService.Api.MatchesWonAsync(item.Value.BackendPlayerId, 1);
                    }
                    await ApiService.Api.LevelsAsync(item.Value.BackendPlayerId, levelChange);
                    await ApiService.Api.EloAsync(item.Value.BackendPlayerId, eloChange);
                    await ApiService.Api.MatchesPlayedAsync(item.Value.BackendPlayerId, 1);
                }
                Debug.Log($"[MatchLifecycle] Match {slot.MatchId} ended in GameServerManager.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MatchLifecycle] Failed to end match {slot.MatchId}: {e.Message}");
            }
            finally
            {
                MatchStateRegistry.CleanupSlot(slot.SlotIndex);
            }
        }
    }
}
