using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Tags;
using Unity.Burst;
using Unity.NetCode;
using Unity.Collections;
using Unity.Entities;
using Assets.Scripts.ECS.Components.Projectiles;
using UnityEngine;

namespace Assets.Scripts
{
    partial struct PlayerDeathSystem : ISystem
    {
        private HitEffectRegistryComponent _hitEffectRegistry;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton(out _hitEffectRegistry))
            {
                Debug.LogError("[PlayerDeathSystem] HitEffectRegistry not found");
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (health,player) in SystemAPI
                .Query<RefRO<HealthComponent>>()
                .WithAll<PlayerShipTag>()
                .WithNone<PlayerDeathComponent>()
                .WithEntityAccess())
            {
                if(health.ValueRO.Value <=0)
                {
                    ecb.AddComponent(player, new PlayerDeathComponent() { TimeOfDeath = SystemAPI.Time.ElapsedTime });
                    ecb.AddComponent<DeadTag>(player);
                    ecb.Instantiate(_hitEffectRegistry.DestructionEffectPrefab);
                }
            }

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }
    }
}
