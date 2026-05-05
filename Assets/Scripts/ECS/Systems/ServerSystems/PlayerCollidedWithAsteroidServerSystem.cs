using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Collision;
using Assets.Scripts.ECS.Components.Tags;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CollisionServerSystem))]
    public partial struct PlayerCollidedWithAsteroidServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (collisionEvents, health) in SystemAPI
                .Query<DynamicBuffer<CollisionEventBufferElement>, RefRW<HealthComponent>>()
                .WithAll<PlayerShipTag>()
                .WithNone<DeadTag>()
                .WithNone<PlayerDeathComponent>())
            {
                for (int i = 0; i < collisionEvents.Length; i++)
                {
                    if (collisionEvents[i].OtherLayer == CollisionLayer.Asteroid)
                    {
                        health.ValueRW.Value = 0;
                        break;
                    }
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
