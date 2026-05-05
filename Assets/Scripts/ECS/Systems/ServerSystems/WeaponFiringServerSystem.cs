using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Input;
using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.Networking.RPCs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct WeaponFiringServerSystem : ISystem
    {
        private const float _ownerGracePeriod = 0.15f;
        private uint _projectileIdCounter;

        public void OnCreate(ref SystemState state) => _projectileIdCounter = 0;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            bool isServer = state.WorldUnmanaged.IsServer();

            foreach (var (transform, input, ghostOwner, entity) in SystemAPI
                .Query<RefRO<LocalTransform>, RefRO<ShipInputComponent>, RefRO<GhostOwner>>()
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

                    var projectile = ecb.Instantiate(w.ServerProjectilePrefab);
                    ecb.SetComponent(projectile, LocalTransform.FromPositionRotation(
                        worldFiringPos,
                        transform.ValueRO.Rotation
                    ));

                    ecb.SetComponent(projectile, new ServerProjectileComponent
                    {
                        TimeAlive = 0,
                        Owner = entity,
                        Speed = w.ProjectileSpeed,
                        Damage = w.ProjectileDamage,
                        Lifetime = w.ProjectileLifetime,
                        DamageType = w.ProjectileDamageType,
                        OwnerGracePeriod = _ownerGracePeriod,
                        NetworkId = ++_projectileIdCounter
                    });

                    var rpc = ecb.CreateEntity();
                    ecb.AddComponent(rpc, new ProjectileFiredRPC
                    {
                        FiringNetworkId = ghostOwner.ValueRO.NetworkId,
                        WeaponTypeId = w.WeaponTypeId,
                        Position = worldFiringPos,
                        Rotation = transform.ValueRO.Rotation,
                        ProjectileId = _projectileIdCounter
                    });

                    ecb.AddComponent(rpc, new SendRpcCommandRequest());

                    w.FireCooldown = w.FireRate;
                    weapons[i] = w;
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}