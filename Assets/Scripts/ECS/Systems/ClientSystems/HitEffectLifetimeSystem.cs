using Assets.Scripts.ECS.Components.Projectiles;
using Unity.Burst;
using Unity.Entities;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct HitEffectLifetimeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (effect, entity) in SystemAPI
                .Query<RefRW<HitEffectComponent>>()
                .WithEntityAccess())
            {
                effect.ValueRW.TimeAlive -= dt;
                if (effect.ValueRO.TimeAlive <= 0)
                    ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
