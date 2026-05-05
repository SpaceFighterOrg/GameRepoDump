using Assets.Scripts.MVVM.Models.DamageTypes;
using Unity.Entities;

namespace Assets.Scripts.ECS.Buffers
{
    public struct WeaponRegistryBufferElement : IBufferElementData
    {
        public int WeaponTypeId;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public int ProjectileDamage;
        public DamageType ProjectileDamageType;
        public Entity ClientProjectilePrefab;
        public Entity ServerProjectilePrefab;
    }
}