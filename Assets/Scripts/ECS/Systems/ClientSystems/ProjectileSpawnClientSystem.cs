using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.Networking.RPCs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ProjectileSpawnClientSystem : ISystem
    {
        private const float _maxVisualRange = 500f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            if (!SystemAPI.TryGetSingleton<NetworkId>(out var networkId))
                return;

            if (!SystemAPI.HasSingleton<WeaponRegistryBufferElement>())
                return;

            var registry = SystemAPI.GetSingletonBuffer<WeaponRegistryBufferElement>(true);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            float3 localShipPos = float3.zero;
            foreach (var transform in SystemAPI
                .Query<RefRO<LocalTransform>>()
                .WithAll<PlayerShipTag, GhostOwnerIsLocal>())
            {
                localShipPos = transform.ValueRO.Position;
                break;
            }

            foreach (var (rpc, _, entity) in SystemAPI
                .Query<RefRO<ProjectileFiredRPC>, RefRO<ReceiveRpcCommandRequest>>()
                .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);

                if (rpc.ValueRO.FiringNetworkId == networkId.Value)
                {
                    AssignNetworkIdToLocalVisual(ref state, ref ecb, rpc.ValueRO.Position, rpc.ValueRO.ProjectileId);
                    continue;
                }

                var spawnPos = rpc.ValueRO.Position;

                if (math.distance(localShipPos, spawnPos) > _maxVisualRange)
                    continue;

                var weaponData = registry[rpc.ValueRO.WeaponTypeId];
                var visual = ecb.Instantiate(weaponData.ClientProjectilePrefab);

                ecb.SetComponent(visual, LocalTransform.FromPositionRotation(
                    spawnPos,
                    rpc.ValueRO.Rotation
                ));

                ecb.AddComponent(visual, new ClientProjectileComponent
                {
                    Speed = weaponData.ProjectileSpeed,
                    Lifetime = weaponData.ProjectileLifetime,
                    TimeAlive = 0f,
                    NetworkId = rpc.ValueRO.ProjectileId
                });
            }

            ecb.Playback(state.EntityManager);
        }

        private void AssignNetworkIdToLocalVisual(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            float3 firingPos,
            uint networkId)
        {
            Entity closest = Entity.Null;

            foreach (var (projectile, entity) in SystemAPI
                .Query<RefRO<ClientProjectileComponent>>()
                .WithNone<ProjectileTrackedTag>()
                .WithEntityAccess())
            {
                if (projectile.ValueRO.NetworkId != 0) continue;

                var comp = SystemAPI.GetComponentRW<ClientProjectileComponent>(entity);
                comp.ValueRW.NetworkId = networkId;
                return;
            }
        }
    }
}