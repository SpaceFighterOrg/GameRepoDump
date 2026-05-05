using Assets.Scripts.ECS.Components.Projectiles;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public class HitEffectRegistryAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject DefaultHitEffectPrefab;
        [SerializeField] private GameObject KineticHitEffectPrefab;
        [SerializeField] private GameObject IonHitEffectPrefab;
        [SerializeField] private GameObject DestructionEffectPrefab;

        private class Baker : BakerBase<HitEffectRegistryAuthoring>
        {
            public override void Bake(HitEffectRegistryAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new HitEffectRegistryComponent
                {
                    DefaultHitEffectPrefab = GetEntity(authoring.DefaultHitEffectPrefab, TransformUsageFlags.Dynamic),
                    IonHitEffectPrefab = GetEntity(authoring.IonHitEffectPrefab, TransformUsageFlags.Dynamic),
                    KineticHitEffectPrefab = GetEntity(authoring.KineticHitEffectPrefab, TransformUsageFlags.Dynamic),
                    DestructionEffectPrefab = GetEntity(authoring.DestructionEffectPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}