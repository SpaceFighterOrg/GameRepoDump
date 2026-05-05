using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Input;
using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.ECS.Components.Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct WeaponFiringClientSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick)
                return;

            var dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, input, entity) in SystemAPI
                .Query<RefRO<LocalTransform>, RefRO<ShipInputComponent>>()
                .WithAll<PlayerShipTag, Simulate, WeaponBufferElement>()
                .WithEntityAccess())
            {
                var weapons = SystemAPI.GetBuffer<WeaponBufferElement>(entity);

                for (int i = 0; i < weapons.Length; i++)
                {
                    var w = weapons[i];
                    w.FireCooldown -= dt;

                    bool wantsToFire = (input.ValueRO.WeaponFireMask & (1u << i)) != 0;

                    if (!wantsToFire || w.FireCooldown > 0f)
                    {
                        weapons[i] = w;
                        continue;
                    }

                    var worldFiringPos = transform.ValueRO.Position +
                         math.rotate(transform.ValueRO.Rotation, w.FiringPointOffset);

                    var visualProjectileEntity = ecb.Instantiate(w.ClientProjectilePrefab);
                    ecb.SetComponent(visualProjectileEntity, LocalTransform.FromPositionRotation(
                        worldFiringPos,
                        transform.ValueRO.Rotation
                    ));

                    ecb.AddComponent(visualProjectileEntity, new ClientProjectileComponent
                    {
                        TimeAlive = 0,
                        Speed = w.ProjectileSpeed,
                        Lifetime = w.ProjectileLifetime,
                        NetworkId = 0
                    });

                    w.FireCooldown = w.FireRate;
                    weapons[i] = w;
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}