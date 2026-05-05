using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Collision;
using Assets.Scripts.ECS.Components.Spawners;
using Assets.Scripts.ECS.Components.Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.PredictedSystems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct AsteroidSpawnerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AsteroidSpawnerComponent>();
            state.RequireForUpdate<AsteroidSpawnReadyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (spawner, registry, entity) in SystemAPI
                .Query<RefRO<AsteroidSpawnerComponent>, DynamicBuffer<AsteroidRegistryBufferElement>>()
                .WithAll<AsteroidSpawnReadyTag>()
                .WithEntityAccess())
            {
                if (registry.Length == 0)
                    continue;

                var rng = new Random(spawner.ValueRO.Seed == 0 ? 1u : spawner.ValueRO.Seed);
                var slotIndex = spawner.ValueRO.MatchSlotIndex;

                for (int i = 0; i < spawner.ValueRO.Count; i++)
                {
                    var prefab = registry[rng.NextInt(0, registry.Length)].Prefab;
                    var asteroid = ecb.Instantiate(prefab);

                    var direction = rng.NextFloat3Direction();
                    var dist = rng.NextFloat(0f, spawner.ValueRO.SpawnRadius);
                    var position = direction * dist; var rotation = rng.NextQuaternionRotation();
                    var scale = rng.NextFloat(spawner.ValueRO.ScaleMin, spawner.ValueRO.ScaleMax);

                    ecb.SetComponent(asteroid, LocalTransform.FromPositionRotationScale(position, rotation, scale));
                    ecb.AddComponent(asteroid, new MatchComponent { MatchId = slotIndex });

                    RecalculateBuffers(ref state,ref ecb, ref prefab, ref asteroid, scale);
                }

                ecb.RemoveComponent<AsteroidSpawnerComponent>(entity);
            }

            ecb.Playback(state.EntityManager);
        }

        private void RecalculateBuffers(ref SystemState state, ref EntityCommandBuffer ecb, ref Entity prefab, ref Entity asteroid, float scale)
        {
            var prefabBuffer = SystemAPI.GetBuffer<ColliderShapeBufferElement>(prefab);
            var newBuffer = ecb.SetBuffer<ColliderShapeBufferElement>(asteroid);

            for (int j = 0; j < prefabBuffer.Length; j++)
            {
                var shape = prefabBuffer[j];
                shape.LocalOffset *= scale;

                switch (shape.ShapeType)
                {
                    case ColliderShapeType.Sphere:
                        shape.Params.x *= scale;
                        break;
                    case ColliderShapeType.Box:
                        shape.Params *= scale;
                        break;
                    case ColliderShapeType.Capsule:
                        shape.Params.x *= scale;
                        shape.Params.y *= scale;
                        break;
                }

                newBuffer.Add(shape);
            }

            var prefabBounding = SystemAPI.GetComponent<BoundingRadiusComponent>(prefab);
            ecb.SetComponent(asteroid, new BoundingRadiusComponent
            {
                Value = prefabBounding.Value * scale
            });
        }
    }
}
