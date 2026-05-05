using Assets.Scripts.MVVM.Models.DamageTypes;
using Unity.Entities;

namespace Assets.Scripts.ECS.Components.Projectiles
{
    public struct HitEffectRegistryComponent : IComponentData
    {
        public Entity DefaultHitEffectPrefab;
        public Entity IonHitEffectPrefab;
        public Entity KineticHitEffectPrefab;
        public Entity DestructionEffectPrefab;

        public Entity GetHitEffectType(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Kinetic => KineticHitEffectPrefab,
                DamageType.Ion => IonHitEffectPrefab,
                _ => DefaultHitEffectPrefab
            };
        }
    }
}