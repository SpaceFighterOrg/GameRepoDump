using Assets.Scripts.ECS.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(MatchLifecycleSystem))]
    public partial struct PlayerDisconnectSystem : ISystem
    {
        private EntityQuery _disconnectedQuery;

        public void OnCreate(ref SystemState state)
        {
            _disconnectedQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ConnectionState>(),
                ComponentType.Exclude<NetworkId>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_disconnectedQuery.IsEmpty) return;

            var shipByNetId = new NativeHashMap<uint, Entity>(8, Allocator.Temp);
            foreach (var (cleanup, entity) in SystemAPI.Query<RefRO<PlayerConnectionCleanup>>()
                .WithAll<PlayerShipTag>()
                .WithEntityAccess())
            {
                shipByNetId[cleanup.ValueRO.NetworkId] = entity;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (connState, entity) in SystemAPI.Query<RefRO<ConnectionState>>()
                .WithNone<NetworkId>()
                .WithEntityAccess())
            {
                var netId = (uint)connState.ValueRO.NetworkId;

                ecb.RemoveComponent<ConnectionState>(entity);

                if (!shipByNetId.TryGetValue(netId, out var shipEntity)) continue;

                ecb.DestroyEntity(shipEntity);
                Debug.Log($"[PlayerDisconnect] networkId={netId} disconnected.");
            }

            shipByNetId.Dispose();
            ecb.Playback(state.EntityManager);
        }
    }
}
