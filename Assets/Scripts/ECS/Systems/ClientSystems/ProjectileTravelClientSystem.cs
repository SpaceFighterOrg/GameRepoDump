
using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.ECS.Components.Tags;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ProjectileTravelClientSystem : ISystem
    {
        private const float _maxVisualRange = 300f;

        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            float3 localShipPos = float3.zero;
            foreach (var transform in SystemAPI
                .Query<RefRO<LocalTransform>>()
                .WithAll<PlayerShipTag, GhostOwnerIsLocal>())
            {
                localShipPos = transform.ValueRO.Position;
                break;
            }

            foreach (var (transform, projectile, entity) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<ClientProjectileComponent>>()
                .WithEntityAccess())
            {
                projectile.ValueRW.TimeAlive += dt;

                if (projectile.ValueRO.TimeAlive >= projectile.ValueRO.Lifetime)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (math.distance(localShipPos, transform.ValueRO.Position) > _maxVisualRange)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                transform.ValueRW.Position +=
                    math.forward(transform.ValueRO.Rotation) *
                    projectile.ValueRO.Speed * dt;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}