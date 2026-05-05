using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.ECS.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class PlayerShipDestroyedSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingleton<HitEffectRegistryComponent>(out var registry))
                return;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<PlayerShipTag>()
                         .WithNone<ShipDestructionCleanupComponent>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new ShipDestructionCleanupComponent
                {
                    LastPosition = transform.ValueRO.Position,
                    
                });
            }

            foreach (var (transform, cleanup) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<ShipDestructionCleanupComponent>>()
                     .WithAll<PlayerShipTag>())
            {
                cleanup.ValueRW.LastPosition = transform.ValueRO.Position;
            }

            foreach (var (cleanup, entity) in
                SystemAPI.Query<RefRO<ShipDestructionCleanupComponent>>()
                .WithNone<PlayerShipTag>()
                .WithEntityAccess())
            {
                var effect = ecb.Instantiate(registry.DestructionEffectPrefab);
                ecb.SetComponent(effect, LocalTransform.FromPosition(cleanup.ValueRO.LastPosition));

                ecb.RemoveComponent<ShipDestructionCleanupComponent>(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
