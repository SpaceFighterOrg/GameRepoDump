
using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.Networking.RPCs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ProjectileTravelServerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, projectile, entity) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<ServerProjectileComponent>>()
                .WithEntityAccess())
            {
                projectile.ValueRW.TimeAlive += dt;

                if (projectile.ValueRO.TimeAlive >= projectile.ValueRO.Lifetime)
                {
                    var rpc = ecb.CreateEntity();
                    ecb.AddComponent(rpc, new ProjectileDestroyedRPC
                    {
                        ProjectileId = projectile.ValueRO.NetworkId,
                        Position = transform.ValueRO.Position,
                        DamageType = projectile.ValueRO.DamageType,
                        DestroyReason = ProjectileDestroyReason.Lifetime
                    });
                    ecb.AddComponent(rpc, new SendRpcCommandRequest());
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