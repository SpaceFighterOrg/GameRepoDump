using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Collision;
using Assets.Scripts.ECS.Components.Input;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.MVVM.Models.Ships;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public class ShipAuthoring : MonoBehaviour
    {
        public ShipData shipData;

        public class Baker : BakerBase<ShipAuthoring>
        {
            public override void Bake(ShipAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

                AddComponent(entity, new PlayerShipTag());

                AddComponent(entity, new HealthComponent { Value = authoring.shipData.MaxHealth, MaxValue = authoring.shipData.MaxHealth });
                if (authoring.shipData.MaxShield > 0)
                {
                    AddComponent(entity, new ShieldComponent
                    {
                        Value = authoring.shipData.MaxShield,
                        RegenRate = authoring.shipData.ShieldRechargeRate,
                        MaxValue = authoring.shipData.MaxShield,
                        TimeSinceLastHit = 0,
                        RegenValue = 10
                    });
                }

                AddComponent(entity, new VelocityComponent { 
                    MaxVelocity = authoring.shipData.MaxSpeed, 
                    Velocity = 1,
                    Acceleration = authoring.shipData.Acceleration,
                    Deceleration = authoring.shipData.Deceleration
                });

                AddComponent(entity, new RotationVelocityComponent { Value = authoring.shipData.MaxRotationSpeed });

                AddComponent(entity, LocalTransform.FromPositionRotationScale(authoring.transform.position, authoring.transform.rotation, 1));

                AddComponent(entity, new ShipInputComponent());

                AddComponent(entity, new TeamComponent());
                AddComponent(entity, new UsernameComponent());

                AddWeapons(authoring, entity);
                AddColliders(entity, authoring, CollisionLayer.Ship, CollisionLayers.ShipCollisionMask);
            }

            private void AddWeapons(ShipAuthoring authoring, Entity entity)
            {
                var weaponMounts = authoring.GetComponentsInChildren<WeaponAuthoring>();
                var buffer = AddBuffer<WeaponBufferElement>(entity);

                foreach (var mount in weaponMounts)
                {
                    var weaponType = mount.WeaponType;
                    var localPos = authoring.transform.InverseTransformPoint(mount.transform.position);

                    buffer.Add(new WeaponBufferElement
                    {
                        WeaponTypeId = weaponType.Id,
                        FireRate = weaponType.FireRate,
                        FireCooldown = 0,

                        ProjectileDamage = weaponType.ProjectileType.Damage,
                        ProjectileDamageType = weaponType.ProjectileType.DamageType,
                        ProjectileLifetime = weaponType.ProjectileType.Lifetime,
                        ProjectileSpeed = weaponType.ProjectileType.Speed,
                        ClientProjectilePrefab = GetEntity(weaponType.ProjectileType.ClientPrefab, TransformUsageFlags.Dynamic),
                        ServerProjectilePrefab = GetEntity(weaponType.ProjectileType.ServerPrefab, TransformUsageFlags.Dynamic),

                        FiringPointOffset = localPos,
                    });
                }
            }
        }
    }
}