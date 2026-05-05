using UnityEngine;
using Unity.Entities;
using Assets.Scripts.ECS.Components.Projectiles;

namespace Assets.Scripts.Authorings
{
    public class HitEffectAuthoring : MonoBehaviour
    {
        [SerializeField] private float Lifetime;

        private class Baker : BakerBase<HitEffectAuthoring>
        {
            public override void Bake(HitEffectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new HitEffectComponent
                {
                    TimeAlive = authoring.Lifetime
                });
            }
        }
    }
}