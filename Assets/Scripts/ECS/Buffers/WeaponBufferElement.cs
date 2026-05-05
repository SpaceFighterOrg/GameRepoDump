using Assets.Scripts.MVVM.Models.DamageTypes;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS.Buffers
{
    public struct WeaponBufferElement : IBufferElementData
    {
        public int WeaponTypeId;

        public float FireRate;
        public float FireCooldown;

        public float ProjectileSpeed;
        public int ProjectileLifetime;
        public int ProjectileDamage;
        public DamageType ProjectileDamageType;
        public Entity ClientProjectilePrefab;
        public Entity ServerProjectilePrefab;

        public float3 FiringPointOffset;
        public quaternion FiringPointRotation;
    }
}